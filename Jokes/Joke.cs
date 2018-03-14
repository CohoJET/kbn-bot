using System.Collections.Generic;

namespace KBNBot.Jokes
{
    class Joke
    {
        public int ID { get; set; }
        public string[] Keys { get; set; }
        public List<string> Jokes { get; set; }

        public Joke() { }
        public Joke(string[] keys)
        {
            Keys = keys;
            Jokes = new List<string>();
        }
    }
}
