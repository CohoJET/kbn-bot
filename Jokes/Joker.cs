using LiteDB;
using System;
using System.Linq;

namespace KBNBot.Jokes
{
    class Joker
    {
        private const int MIN_JOKE_SIZE = 20;

        // Link to the DB with jokes
        private LiteDatabase db;
        // Actual jokes
        LiteCollection<Joke> jokes;

        public Joker(LiteDatabase db)
        {
            this.db = db;
            jokes = db.GetCollection<Joke>("jokes");
        }

        public void TrackJoke(string[] keys)
        {
            var joke = new Joke(keys);
            jokes.Insert(joke);
        }

        public bool CheckJokes(string text)
        {
            if (text.Length < MIN_JOKE_SIZE)
                return false;

            text = text.ToLower().Replace('"', ' ').Replace('.', ' ').Replace('-', ' ').Replace('?', ' ');
            //Console.WriteLine(text);

            var count = jokes.Count();
            for(int i = 0; i < count; i++)
            {
                var joke = jokes.Find(j => j.ID == i + 1).First();
                foreach (var key in joke.Keys)
                {
                    if (text.ToLower().Contains(string.Format(" {0} ", key.ToLower())) || text.Contains(string.Format("{0} ", key.ToLower())) || text.Contains(string.Format("{0} ", key.ToLower())))
                    {
                        joke.Jokes.Add(text);
                        jokes.Update(joke);
                        return true;
                    }
                }
            }
            return false;
        }

        public string DumpJokes()
        {
            string dump = string.Empty;
            var count = jokes.Count();
            for (int i = 0; i < count; i++)
            {
                dump += "====================" + Environment.NewLine;
                var joke = jokes.Find(j => j.ID == i + 1).First();
                foreach (var key in joke.Keys)
                    dump += key + " ";
                dump += Environment.NewLine + Environment.NewLine;
                foreach (var aJoke in joke.Jokes)
                    dump += aJoke + Environment.NewLine + Environment.NewLine;
                dump += "====================" + Environment.NewLine;
            }
            return dump;
        }
    }
}
