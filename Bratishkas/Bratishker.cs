using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KBNBot.Bratishkas
{
    class Bratishker
    {
        // Database.
        private LiteDatabase db;
        // Random numbers generator for quotes selection.
        private Random random = new Random();
        // Bratishkas pictures library.
        private string libraryPath;
        private List<string> pictureNames;
        // Bratishkas quotes library.
        private LiteCollection<Quote> quotes;

        public Bratishker(string libraryPath, LiteDatabase db)
        {
            this.libraryPath = libraryPath;
            this.db = db;
            // Load Bratishkas pictures.
            pictureNames = new List<string>();
            var files = Directory.GetFiles(libraryPath);
            foreach (string file in files)
                pictureNames.Add(Path.GetFileName(file));
            // Load Bratishkas quotes.
            quotes = db.GetCollection<Quote>("quotes");
        }

        public Tuple<string,string> GetRandomBratishka()
        {
            var picturePath = libraryPath + pictureNames[random.Next(pictureNames.Count)];
            var count = quotes.Count();
            var id = random.Next(count);
            var quote = quotes.Find(q => q.ID == id).FirstOrDefault();
            return new Tuple<string, string>(picturePath, quote.Text);
        }

        public void AddQuote(string text)
        {
            quotes.Insert(new Quote(text));
        }
    }
}
