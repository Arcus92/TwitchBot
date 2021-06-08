using System.Text;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-01-30: A response message. This can contain multiple variations.
    /// </summary>
    public struct ResponseMessage : IResponseMessage
    {
        /// <summary>
        /// Creates a response message
        /// </summary>
        /// <param name="message"></param>
        public ResponseMessage(string message)
        {
            Messages = new string[] { message };
        }

        /// <summary>
        /// Creates a response message
        /// </summary>
        /// <param name="messages"></param>
        public ResponseMessage(string[] messages)
        {
            Messages = messages;
        }

        /// <summary>
        /// Gets all variations of the message
        /// </summary>
        public string[] Messages { get; }

        /// <summary>
        /// Returns the text of the response message
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public string GetText(OnResponseMessageParameterHandler handler = null)
        {
            // Gets a random message
            var message = TwitchBotApp.GetRandomText(Messages);

            return HandleMessageParameter(message, handler);
        }

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static string HandleMessageParameter(string message, OnResponseMessageParameterHandler handler = null)
        {
            // There is no handler. Just return the message.
            if (handler == null)
                return message;

            // No message.
            if (message == null)
                return null;

            var builder = new StringBuilder();
            var pos = 0;
            while (pos < message.Length)
            {
                var start = message.IndexOf('{', pos);
                var end = start >= 0 ? message.IndexOf('}', start) : -1;
                if (end >= 0)
                {
                    // Adds the part before the parameter
                    builder.Append(message.Substring(pos, start - pos));

                    // Reads the argument
                    string argument = null;
                    var argumentIndex = message.IndexOf(':', start);
                    if (argumentIndex > end)
                        argumentIndex = -1;

                    // Reads the argument
                    if (argumentIndex >= 0)
                    {
                        argument = message.Substring(argumentIndex + 1, end - argumentIndex - 1);
                    }
                    else
                    {
                        argumentIndex = end;
                    }

                    // Reads the name of the variable
                    var name = message.Substring(start + 1, argumentIndex - start - 1).ToLowerInvariant();
                    var value = handler.Invoke(name, argument);
                    if (value == null)
                        value = message.Substring(start, end - start + 1);

                    builder.Append(value);

                    pos = end + 1;
                }
                else
                {
                    // Add the rest of the message
                    builder.Append(message.Substring(pos));
                    pos = message.Length;
                }
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// The response message parameter handler is used to insert the parameter values
    /// </summary>
    /// <param name="name"></param>
    /// <param name="argument"></param>
    /// <returns></returns>
    public delegate string OnResponseMessageParameterHandler(string name, string argument);
}
