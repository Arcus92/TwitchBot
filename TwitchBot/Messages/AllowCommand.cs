using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-02-14: The command to allow a user to bypass any <see cref="ModeratorCommand"/>.
    /// </summary>
    public class AllowCommand : CommandHandler
    {
        /// <summary>
        /// Creates the allow command
        /// </summary>
        public AllowCommand() : base("allow")
        {
        }

        /// <summary>
        /// Gets and sets the message when the command is executed successfully
        /// </summary>
        public ResponseMessageLogic MessageSuccess { get; set; }

        /// <summary>
        /// Handles the command
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected override bool HandleCommand(TwitchBotApp app, ChatMessage message, string arguments)
        {
            // Allows the user
            if (!string.IsNullOrEmpty(arguments))
            {
                if (message.IsBroadcaster || message.IsModerator)
                {
                    // Sends the message
                    app.SendMessage(MessageSuccess, (name, argument) =>
                    {

                        switch (name)
                        {
                            case "username":
                                return arguments;
                            default:
                                return app.HandleMessageParameter(name, argument);
                        }
                    });

                    // Adds the user to the allow list
                    app.AllowedUsers.Add(arguments);
                    return true;
                }
                
            }
            return false;
        }
    }
}
