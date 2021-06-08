namespace TwitchBot
{
    /// <summary>
    /// DS 2020-01-30: The entry point
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The main entry point
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var app = new TwitchBotApp();
            app.Run();
        }
    }
}
