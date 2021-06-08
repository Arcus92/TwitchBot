namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-02-13: A complex message response where you can use conditions to program a response logic
    /// </summary>
    public struct ResponseMessageLogic : IResponseMessage
    {
        /// <summary>
        /// Creates a response message logic with just one message
        /// </summary>
        /// <param name="messages"></param>
        public ResponseMessageLogic(ResponseMessageLogicElement[] messages)
        {
            Messages = messages;
        }

        /// <summary>
        /// Creates a response message logic with just one message
        /// </summary>
        /// <param name="message"></param>
        public ResponseMessageLogic(ResponseMessage message)
        {
            Messages = new ResponseMessageLogicElement[] { new ResponseMessageLogicElement(message) };
        }

        /// <summary>
        /// Creates a response message logic with just one message
        /// </summary>
        /// <param name="message"></param>
        public ResponseMessageLogic(string message)
        {
            Messages = new ResponseMessageLogicElement[] { new ResponseMessageLogicElement(new ResponseMessage(message)) };
        }

        /// <summary>
        /// Gets and sets the main language
        /// </summary>
        public ResponseMessageLogicElement[] Messages { get; set; }

        /// <summary>
        /// Returns the text of the response message
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public string GetText(OnResponseMessageParameterHandler handler = null)
        {
            if (Messages == null)
                return null;

            // Check every message in this order
            foreach (var element in Messages)
            {
                if (element.CheckCondition(handler))
                {
                    return element.Message.GetText(handler);
                }
            }
            return null;
        }
    }

    /// <summary>
    /// DS 2021-02-13: A message with a condition. If the condition is met the message will be executed.
    /// </summary>
    public struct ResponseMessageLogicElement
    {
        /// <summary>
        /// Creates the logic element without any condition
        /// </summary>
        /// <param name="message"></param>
        public ResponseMessageLogicElement(ResponseMessage message)
        {
            Message = message;
            Condition = null;
        }

        /// <summary>
        /// Creates the logic element with a condition
        /// </summary>
        /// <param name="message"></param>
        /// <param name="condition"></param>
        public ResponseMessageLogicElement(ResponseMessage message, string condition)
        {
            Message = message;
            Condition = condition;

            // Compiles the condition
            Logic.CompileCondition(condition);
        }

        /// <summary>
        /// Gets and sets the main language
        /// </summary>
        public ResponseMessage Message { get; set; }

        /// <summary>
        /// Gets and sets the condition
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Checks the condition
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public bool CheckCondition(OnResponseMessageParameterHandler handler)
        {
            if (string.IsNullOrEmpty(Condition))
                return true;

            if (handler == null)
                return false;

            // Executes the condition
            return Logic.ExecuteCondition(Condition, handler, true);
        }
    }
}
