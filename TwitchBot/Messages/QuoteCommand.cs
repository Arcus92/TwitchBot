using System;
using System.Text.RegularExpressions;
using TwitchLib.Client.Models;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-01-30: The quote command can add a quote 
    /// </summary>
    public class QuoteCommand : CommandHandler
    {
        /// <summary>
        /// Creates the quote command
        /// </summary>
        public QuoteCommand() : base("quote")
        {
        }

        /// <summary>
        /// Gets and sets if all users can add quotes. Otherwise only mods can add quotes.
        /// </summary>
        public bool AllUserCanAddQuotes { get; set; }

        /// <summary>
        /// Gets and sets the message when the command is executed successfully
        /// </summary>
        public ResponseMessageLogic MessageSuccess { get; set; }

        /// <summary>
        /// Gets and sets the message when a user is not allowed to execute the command
        /// </summary>
        public ResponseMessageLogic MessageNotAllowed { get; set; }

        /// <summary>
        /// The RegEx for a quote
        /// </summary>
        private static readonly Regex RegExQuote = new Regex(@"[\""\']?([^\""\']*)[\""\']?\s*-?\s*(.*)\,?\s*([0-9]{1,2}\.[0-9]{1,2}\.[0-9]{4})");

        /// <summary>
        /// Handles the command
        /// </summary>
        /// <param name="app"></param>
        /// <param name="message"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        protected override bool HandleCommand(TwitchBotApp app, ChatMessage message, string arguments)
        {
            
            // Returns a new quote
            if (string.IsNullOrEmpty(arguments))
            {
                if (app.Quotes.Count > 0)
                {
                    var i = TwitchBotApp.GetRandomNumber(app.Quotes.Count);
                    var quote = app.Quotes[i];
                    app.SendMessage($"\"{quote.Text}\" - {quote.Author} {quote.Date.ToString("dd.MM.yyyy")}");
                }
            }
            else // Adds a quote
            {
                // User is allowed
                if (AllUserCanAddQuotes || message.IsModerator || message.IsBroadcaster)
                {
                    // Ignore quotes
                    if (arguments.Length > 2 &&
                       (arguments.StartsWith('\"') && arguments.EndsWith('\"') || 
                        arguments.StartsWith('\'') && arguments.EndsWith('\'')))
                    {
                        arguments = arguments.Substring(1, arguments.Length - 2);
                    }

                    var quote = new Quote();
                    quote.Author = app.Channel;
                    quote.Date = DateTime.Now;
                    quote.Text = arguments;
                    quote.CreatedBy = message.DisplayName;

                    // Match the RegEx
                    var match = RegExQuote.Match(arguments);
                    if (match.Success)
                    {
                        quote.Text = match.Groups[1].Value;
                        quote.Author = match.Groups[2].Value;
                        var date = match.Groups[3].Value;
                        if (DateTime.TryParse(date, out var dateTime))
                            quote.Date = dateTime;
                    }

                    

                    // Adds the quote
                    app.AddQuote(quote);

                    // Sends the message
                    app.SendMessage(MessageSuccess);
                }
                else
                {
                    // Sends the message
                    app.SendMessage(MessageNotAllowed);
                }
            }

            return true;
        }
    }
}
