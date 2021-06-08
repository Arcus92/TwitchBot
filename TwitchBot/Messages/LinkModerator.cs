using System.Text.RegularExpressions;
using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-02-14: The moderator command for link detection
    /// </summary>
    public class LinkModerator : ModeratorCommand
    {
        /// <summary>
        /// The RegEx for URLs
        /// </summary>
        private static readonly Regex RegExURL = new Regex("[a-zA-Z0-9\\-]+\\.(de|com|net|ru|org|ch|nl|jp|tv)"); 

        /// <summary>
        /// The RegEx for IPs
        /// </summary>
        private static readonly Regex RegExIP = new Regex("(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)");

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected override bool HandleMessage(ChatMessage message)
        {
            // Checks for URLs
            if (RegExURL.IsMatch(message.Message))
                return true;

            // Checks for IPs
            if (RegExIP.IsMatch(message.Message))
                return true;

            return false;
        }
    }
}
