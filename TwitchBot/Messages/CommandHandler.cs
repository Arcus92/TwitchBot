using System;
using System.Linq;
using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-01-30: The command handler
    /// </summary>
    public abstract class CommandHandler : IMessageHandler
    {
        /// <summary>
        /// Creates a command handler
        /// </summary>
        public CommandHandler()
        {
        }

        /// <summary>
        /// Creates a command handler
        /// </summary>
        /// <param name="keyWord"></param>
        public CommandHandler(string keyWord)
        {
            KeyWords = new string[] { keyWord };
        }

        /// <summary>
        /// Creates a command handler
        /// </summary>
        /// <param name="keyWords"></param>
        public CommandHandler(string[] keyWords)
        {
            KeyWords = keyWords;
        }

        /// <summary>
        /// The command key words
        /// </summary>
        public string[] KeyWords { get; set; }

        /// <summary>
        /// The default timeout
        /// </summary>
        public const int DefaultTimeout = 30;

        /// <summary>
        /// Gets and sets the timeout of the command
        /// </summary>
        public TimeSpan Timeout { get; set; } = new TimeSpan(0, 0, DefaultTimeout);

        /// <summary>
        /// Gets and sets the timeout condition
        /// </summary>
        public string TimeoutCondition { get; set; }

        /// <summary>
        /// The date of the last usage
        /// </summary>
        private DateTime m_LastUsage;

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Handle(TwitchBotApp app, ChatMessage message)
        {
            // This is a command
            if (message.Message.StartsWith('!'))
            {
                // Gets the key word
                var index = message.Message.IndexOf(' ');
                if (index == -1) index = message.Message.Length;

                var keyWord = message.Message.Substring(1, index - 1);

                if (KeyWords.Any(k => k.Equals(keyWord, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // Check the timeout
                    var now = DateTime.Now;
                    if (m_LastUsage + Timeout > now)
                    {
                        // Checks the timeout condition
                        if (Logic.ExecuteCondition(TimeoutCondition, app.HandleMessageParameter(message, null), true))
                        {
                            Console.WriteLine($" Ignore !{keyWord} command due to timeout.");
                            return false;
                        }
                    }

                    // Parse the argument
                    string arguments = null;
                    if (message.Message.Length > keyWord.Length + 1)
                    {
                        arguments = message.Message.Substring(keyWord.Length + 1).Trim();
                    }

                    Console.WriteLine($" Handle !{keyWord} command.");

                    // Execute the command
                    var result = HandleCommand(app, message, arguments);
                    if (result)
                    {
                        m_LastUsage = now;
                    }
                    return result;
                }
            }
            return false;
        }

        /// <summary>
        /// Handles the command
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected abstract bool HandleCommand(TwitchBotApp app, ChatMessage message, string arguments);
    }
}
