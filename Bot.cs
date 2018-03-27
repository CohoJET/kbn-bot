using KBNBot.Jokes;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace KBNBot
{
    sealed class Bot : IDisposable
    {
        private bool isSilent;
        // Gif collection.
        private const int DEFAULT_GIFS_COUNT = 5;
        private const int DEFAULT_GIFS_COUNT_MAX = 25;
        private string libraryPath;
        private List<string> gifNames;
        // Season tip.
        private const int DEFAULT_SEASON_OPENING_DAY = 3;
        private const int DEFAULT_SEASON_OPENING_MONTH = 5;
        // Database
        private LiteDatabase db;
        // Joker to make stupid jokes
        private const int SLEEP_BETWEEN_JOKE_CHECKS = 3;
        private readonly string[] GOTIT_EMOTIONS = new string[] { "😂", "😅", "😆", "😋", "🤣", "😹", "😸", "🙀", "😝", "😃", "😀" };
        private Joker joker;
        private DateTime lastJokeCheck;

        private Random random = new Random();

        private TelegramBotClient client;

        public Bot(string apiToken, bool isSilent)
        {
            // Load GIFs library.
            libraryPath = Directory.GetCurrentDirectory() + @"\Gifs\";
            gifNames = new List<string>();
            var files = Directory.GetFiles(libraryPath);
            foreach (string file in files)
                gifNames.Add(Path.GetFileName(file));
            Console.WriteLine("Library initialized with {0} gifs.", gifNames.Count);
            // Load DB.
            db = new LiteDatabase(@"kbnbot.db");
            // Initialize Joker
            joker = new Joker(db);
            // Initialize Telegram API.
            client = new TelegramBotClient(apiToken);
            client.OnMessage += OnMessage;
            client.StartReceiving();
            Console.WriteLine("Bot is up and running!");

            this.isSilent = isSilent;
        }

        private void OnMessage(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message == null)
                return;

            switch (message.Type)
            {
                case MessageType.ServiceMessage:
                    ProcessGreetings(message);
                    break;
                case MessageType.TextMessage:
                    if (message.Text.StartsWith("/"))
                        ProcessCommand(message);
                    else
                    {
                        if(!isSilent && (DateTime.Now - lastJokeCheck).TotalMinutes > SLEEP_BETWEEN_JOKE_CHECKS && joker.CheckJokes(message.Text))
                        {
                            lastJokeCheck = DateTime.Now;
                            Console.WriteLine("I got a new joke, so funny.");
                            client.SendTextMessageAsync(message.Chat.Id, GOTIT_EMOTIONS[random.Next(GOTIT_EMOTIONS.Length)], ParseMode.Default, false, false, message.MessageId);
                        }
                    }
                    break;
                case MessageType.DocumentMessage:
                    ProcessDocument(message);
                    break;
            }
        }

        private void ProcessGreetings(Message message)
        {
            if (isSilent)
                return;
            if (message.NewChatMember != null || message.NewChatMembers != null)
            {
                Console.WriteLine("Brace yourself! Welcoming flood incoming now!");
                client.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать!");
                Gimme(message.Chat.Id, DEFAULT_GIFS_COUNT_MAX);
            }
        }

        private void ProcessCommand(Message message)
        {
            var command = message.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Remove mention in group chats.
            for (int i = 0; i < command[0].Length; i++)
                if (command[0][i] == '@')
                {
                    command[0] = command[0].Remove(i);
                    break;
                }

            switch (command[0])
            {
                case "/gimme":
                    int count = DEFAULT_GIFS_COUNT;
                    if (command.Length >= 2)
                        int.TryParse(command[1], out count);
                    Console.WriteLine("{0} {1} asked for {2} gifs!", message.From.FirstName, message.From.LastName, count);
                    Gimme(message.Chat.Id, count);
                    break;
                case "/season":
                    Console.WriteLine("{0} {1} wants to ride badly...", message.From.FirstName, message.From.LastName);
                    Season(message.Chat.Id);
                    break;
                case "/trackjoke":
                    var jokeKeys = new string[command.Length - 1];
                    for (int i = 1; i < command.Length; i++)
                        jokeKeys[i - 1] = command[i];
                    Console.WriteLine("{0} {1} is a smartass and come up with a new joke.", message.From.FirstName, message.From.LastName);
                    joker.TrackJoke(jokeKeys);
                    break;
                case "/dumpjokes":
                    client.SendTextMessageAsync(message.Chat.Id, joker.DumpJokes());
                    break;
            }
        }
        private async void Gimme(long chatId, int count)
        {
            if (isSilent)
                return;
            count = count > DEFAULT_GIFS_COUNT_MAX ? DEFAULT_GIFS_COUNT_MAX : count;
            for (int i = 0; i < count; i++)
            {
                var gifName = gifNames[random.Next(gifNames.Count)];
                using (var stream = new FileStream(libraryPath + gifName, System.IO.FileMode.Open))
                {
                    await client.SendDocumentAsync(chatId, new FileToSend(gifName, stream));
                }
            }
        }
        private async void Season(long chatId)
        {
            if (isSilent)
                return;
            var seasonOpening = new DateTime(DateTime.Now.Year, DEFAULT_SEASON_OPENING_MONTH, DEFAULT_SEASON_OPENING_DAY, 0, 0, 0);
            var sadoOpening = new DateTime(DateTime.Now.Year, 4, 15, 0, 0, 0);
            var tillSeason = (seasonOpening - DateTime.Now);
            var tillMaso = (sadoOpening - DateTime.Now);
            if (tillSeason.TotalDays > 0)
                await client.SendTextMessageAsync(chatId, string.Format("До сезона осталось примерно: дней - {0}, часов - {1}, минут - {2}!" + Environment.NewLine + "А для извращенцев: дней - {3}, часов - {4}, минут - {5}!", tillSeason.Days, tillSeason.Hours, tillSeason.Minutes, tillMaso.Days, tillMaso.Hours, tillMaso.Minutes));
        }

        private async void ProcessDocument(Message message)
        {
            var gifName = message.Document.FileName;
            if (gifName == null || gifName.Equals(string.Empty))
                return;
            for (int i = 0; i < gifName.Length; i++)
                if (gifName[i] == '.')
                {
                    gifName = gifName.Substring(i);
                    break;
                }
            gifName = gifNames.Count + gifName;
            gifNames.Add(gifName);
            Console.WriteLine("Ow! I've found a new gif: {0}", gifName);
            using (var stream = new FileStream(libraryPath + gifName, System.IO.FileMode.OpenOrCreate))
            {
                await client.GetFileAsync(message.Document.FileId, stream);
            }
        }

        public void Dispose()
        {
            client.StopReceiving();
        }
    }
}
