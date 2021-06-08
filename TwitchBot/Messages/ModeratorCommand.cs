using System;
using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-02-14: A message handler for moderation tasks.
    /// </summary>
    public abstract class ModeratorCommand : IMessageHandler
    {
        /// <summary>
        /// Gets and sets if the user should be warned first
        /// </summary>
        public bool Warning { get; set; } = true;

        /// <summary>
        /// Gets and sets if the message should also be removed when the user is only warned.
        /// </summary>
        public bool WarningRemoveMessage { get; set; }

        /// <summary>
        /// Gets and sets the warning message
        /// </summary>
        public ResponseMessageLogic MessageWarning { get; set; }

        /// <summary>
        /// Gets and sets the moderate message
        /// </summary>
        public ResponseMessageLogic MessageTimeout { get; set; }

        /// <summary>
        /// Gets and sets the time out for the user 
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Handle(TwitchBotApp app, ChatMessage message)
        {
            // This message was moderated
            if (HandleMessage(message))
            {
                // Ignore the broadcaster and moderators
                if (message.IsBroadcaster || message.IsModerator)
                    return false;

                // Checks if the user is allowed
                if (app.AllowedUsers.Contains(message.Username))
                    return false;

                // Handles the warning
                if (Warning)
                {
                    // The user was not warned
                    if (!app.WarnedUsers.Contains(message.Username))
                    {
                        // Warns the user
                        app.WarnedUsers.Add(message.Username);

                        // Also remove the message
                        if (WarningRemoveMessage)
                        {
                            app.DeleteMessage(message);
                        }

                        // Sends the message
                        app.SendMessage(MessageWarning, message, null);
                        return true;
                    }
                }

                // Builds the message
                var text = MessageTimeout.GetText(app.HandleMessageParameter(message, string.Empty));

                // Delete the message
                app.DeleteMessage(message);

                // Timeout the user
                app.TimeoutUser(message.Username, Timeout, text);
            }
            return false;
        }


        /// <summary>
        /// Returns if this message should be 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected abstract bool HandleMessage(ChatMessage message);
    }
}
