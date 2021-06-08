using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-01-30: The game command
    /// </summary>
    public class GameCommand : CommandHandler
    {
        /// <summary>
        /// Creates the game command
        /// </summary>
        public GameCommand() : base("game")
        {
        }

        /// <summary>
        /// Gets and sets the message when the command is executed successfully
        /// </summary>
        public ResponseMessageLogic MessageSuccess { get; set; }

        /// <summary>
        /// Gets and sets the message when a user is not allowed to execute the command
        /// </summary>
        public ResponseMessageLogic MessageNotAllowed { get; set; }

        /// <summary>
        /// Handles the command
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected override bool HandleCommand(TwitchBotApp app, ChatMessage message, string arguments)
        {
            if (!string.IsNullOrEmpty(arguments))
            {
                // The user is allowed to change the game
                if (message.IsModerator || message.IsBroadcaster)
                {
                    // Gets the game by its name
                    var game = app.GetGameByName(arguments);
                    if (game != null)
                    {
                        var request = new ModifyChannelInformationRequest();
                        request.GameId = game.Id;
                        app.SetChannelInformation(request);

                        // Sends the message that the game was changed
                        app.SendMessage(MessageSuccess,(name, argument) => { 
                            switch(name)
                            {
                                case "game":
                                    return game.Name;
                                default:
                                    return app.HandleMessageParameter(name, argument);
                            }
                        });

                        return true;
                    }
                }
                else
                {
                    // Sends the message that the user can not change the game
                    app.SendMessage(MessageNotAllowed);
                    return true;
                }
            }
            return false;
        }
    }
}
