using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-01-30: A simple command handler that responses with a default message
    /// </summary>
    public class Command : CommandHandler
    {
        /// <summary>
        /// Creates the command handler
        /// </summary>
        public Command() : base()
        {
        }

        /// <summary>
        /// Creates the command handler
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="message"></param>
        public Command(string keyWord, ResponseMessageLogic message) : base(keyWord)
        {
            Message = message;
        }

        /// <summary>
        /// Creates the command handler
        /// </summary>
        /// <param name="keyWords"></param>
        /// <param name="message"></param>
        public Command(string[] keyWords, ResponseMessageLogic message) : base(keyWords)
        {
            Message = message;
        }

        /// <summary>
        /// Gets the response message
        /// </summary>
        public ResponseMessageLogic Message { get; set; }

        /// <summary>
        /// Handles the command
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected override bool HandleCommand(TwitchBotApp app, ChatMessage message, string arguments)
        {
            return app.SendMessage(Message, message, arguments);
        }
    }
}
