namespace KBNBot.Bratishkas
{
    class Quote
    {
        public int ID { get; set; }
        public string Text { get; set; }

        public Quote() { }
        public Quote(string text)
        {
            Text = text;
        }
    }
}
