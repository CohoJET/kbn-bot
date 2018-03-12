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
        // Gif collection.
        private const int DEFAULT_GIFS_COUNT = 5;
        private const int DEFAULT_GIFS_COUNT_MAX = 25;
        private string libraryPath;
        private List<string> gifNames;
        // Season tip.
        private const int DEFAULT_SEASON_OPENING_DAY = 21;
        private const int DEFAULT_SEASON_OPENING_MONTH = 5;

        private Random random = new Random();

        private TelegramBotClient client;

        public Bot(string apiToken)
        {
            // Load GIFs library.
            libraryPath = Directory.GetCurrentDirectory() + @"\Gifs\";
            gifNames = new List<string>();
            var files = Directory.GetFiles(libraryPath);
            foreach (string file in files)
                gifNames.Add(Path.GetFileName(file));
            Console.WriteLine("Library initialized with {0} gifs.", gifNames.Count);
            // Initialize Telegram API.
            client = new TelegramBotClient(apiToken);
            client.OnMessage += OnMessage;
            client.StartReceiving();
            Console.WriteLine("Bot is up and running!");
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
                    ProcessCommand(message);
                    break;
                case MessageType.DocumentMessage:
                    ProcessDocument(message);
                    break;
            }
        }

        private void ProcessGreetings(Message message)
        {
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
                    command[0] = command[0].Remove(i);

            switch (command[0])
            {
                case "/gimme":
                    int count = DEFAULT_GIFS_COUNT;
                    if (command.Length >= 2)
                        int.TryParse(command[1], out count);
                    Console.WriteLine("{0} asked for {1} gifs!", message.From.Username, count);
                    Gimme(message.Chat.Id, count);
                    break;
                case "/season":
                    Console.WriteLine("{0} wants to ride badly...", message.From.Username);
                    Season(message.Chat.Id);
                    break;
            }
        }
        private async void Gimme(long chatId, int count)
        {
            count = count > DEFAULT_GIFS_COUNT_MAX ? DEFAULT_GIFS_COUNT_MAX : count;
            for (int i = 0; i < count; i++)
            {
                var gifName = gifNames[random.Next(gifNames.Count)];
                using (var stream = new FileStream(libraryPath + gifName, FileMode.Open))
                {
                    await client.SendDocumentAsync(chatId, new FileToSend(gifName, stream));
                }
            }
        }
        private async void Season(long chatId)
        {
            var seasonOpening = new DateTime(DateTime.Now.Year, DEFAULT_SEASON_OPENING_MONTH, DEFAULT_SEASON_OPENING_DAY, 0, 0, 0);
            var daysLeft = (seasonOpening - DateTime.Now).TotalDays;
            if (daysLeft > 0)
                await client.SendTextMessageAsync(chatId, string.Format("До сезона осталось ~{0} дней!", (int)daysLeft));
        }

        private void ProcessDocument(Message message)
        {
            var gifName = gifNames.Count + " - " + message.Document.FileName;
            Console.WriteLine("Ow! I've found a new gif: {0}", gifName);
            using (var stream = new FileStream(libraryPath + gifNames.Count + " - " + message.Document.FileName, FileMode.OpenOrCreate))
            {
                client.GetFileAsync(message.Document.FileId, stream);
            }
            gifNames.Add(gifName);
        }

        public void Dispose()
        {
            client.StopReceiving();
        }
    }
}
