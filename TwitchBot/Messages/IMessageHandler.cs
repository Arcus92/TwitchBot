using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-01-30: The message handler
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Handles the chat message
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool Handle(TwitchBotApp app, ChatMessage message);
    }
}
