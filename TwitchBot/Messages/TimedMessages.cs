using System;

namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-02-14: A times message command
    /// </summary>
    public struct TimedMessages
    {
        /// <summary>
        /// Gets and sets the interval of the timed message
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets and sets the messages
        /// </summary>
        public ResponseMessageLogic[] Messages { get; set; }

        /// <summary>
        /// Create the timed messages
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="messages"></param>
        public TimedMessages(TimeSpan interval, ResponseMessageLogic[] messages)
        {
            Interval = interval;
            Messages = messages;
        }
    }
}
