using System;
using System.Configuration;

namespace KBNBot
{
    class Program
    {
        private const string API_TOKEN_SETTINGS_KEY = "apiToken";

        static void Main(string[] args)
        {
            var apiToken = ConfigurationManager.AppSettings.Get(API_TOKEN_SETTINGS_KEY);

            using (var bot = new Bot(apiToken))
            {
                Console.ReadLine();
            }
        }
    }
}
