using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using TwitchBot.Messages;

namespace TwitchBot
{
    /// <summary>
    /// DS 2021-01-31: The command xml file
    /// </summary>
    public class Commands : XmlFile
    {
        /// <summary>
        /// Creates the command settings
        /// </summary>
        public Commands() : base("Commands")
        {
        }

        /// <summary>
        /// Gets the list of message handler
        /// </summary>
        public List<IMessageHandler> MessageHandler { get; private set; } = new List<IMessageHandler>();

        /// <summary>
        /// Gets the message for a new subscriber
        /// </summary>
        public ResponseMessageLogic MessageNewSubscriber { get; private set; }

        /// <summary>
        /// Gets the message for a gifted subscriber
        /// </summary>
        public ResponseMessageLogic MessageGiftedSubscriber { get; private set; }

        /// <summary>
        /// Gets the message for a new follower
        /// </summary>
        public ResponseMessageLogic MessageNewFollower { get; private set; }

        /// <summary>
        /// Gets the message for a raid
        /// </summary>
        public ResponseMessageLogic MessageRaid { get; private set; }

        /// <summary>
        /// Gets the timed message
        /// </summary>
        public TimedMessages TimedMessages { get; private set; }

        /// <summary>
        /// Resets the commands
        /// </summary>
        public void Reset()
        {
            MessageHandler.Clear();
            MessageNewSubscriber = default;
            MessageGiftedSubscriber = default;
            MessageNewFollower = default;
            MessageRaid = default;
        }

        #region Read

        /// <summary>
        /// Reads the settings
        /// </summary>
        /// <param name="xml"></param>
        protected override void Read(XmlReader xml)
        {
            ReadElement(xml, (name) => {
                switch (name)
                {
                    // Reads a simple command
                    case "Command":
                        ReadCommand<Command>(xml, (command) =>
                        {
                            command.Message = ReadMessageLogic(xml);
                        });
                        return true;

                    // Reads a quote command
                    case "QuoteCommand":
                        ReadCommand<QuoteCommand>(xml, (command) =>
                        {
                            ReadElement(xml, (n) => { 
                                switch (n)
                                {
                                    case "MessageSuccess":
                                        command.MessageSuccess = ReadMessageLogic(xml);
                                        return true;

                                    case "MessageNotAllowed":
                                        command.MessageNotAllowed = ReadMessageLogic(xml);
                                        return true;

                                    default:
                                        return false;
                                }
                            });
                        });
                        return true;

                    // Reads a game command
                    case "GameCommand":
                        ReadCommand<GameCommand>(xml, (command) =>
                        {
                            ReadElement(xml, (n) => {
                                switch (n)
                                {
                                    case "MessageSuccess":
                                        command.MessageSuccess = ReadMessageLogic(xml);
                                        return true;

                                    case "MessageNotAllowed":
                                        command.MessageNotAllowed = ReadMessageLogic(xml);
                                        return true;

                                    default:
                                        return false;
                                }
                            });
                        });
                        return true;

                    // Reads a allow command
                    case "AllowCommand":
                        ReadCommand<AllowCommand>(xml, (command) =>
                        {
                            ReadElement(xml, (n) => {
                                switch (n)
                                {
                                    case "MessageSuccess":
                                        command.MessageSuccess = ReadMessageLogic(xml);
                                        return true;

                                    default:
                                        return false;
                                }
                            });
                        });
                        return true;

                    // Reads a link moderator
                    case "LinkModerator":
                        ReadModeratorCommand<LinkModerator>(xml);
                        return true;

                    // Reads the new subscriber message
                    case "NewSubscriber":
                        MessageNewSubscriber = ReadMessageLogic(xml);
                        return true;

                    // Reads the gifted subscriber message
                    case "GiftedSubscriber":
                        MessageGiftedSubscriber = ReadMessageLogic(xml);
                        return true;

                    // Reads the new follower message
                    case "NewFollower":
                        MessageNewFollower = ReadMessageLogic(xml);
                        return true;

                    // Reads the raid
                    case "Raid":
                        MessageRaid = ReadMessageLogic(xml);
                        return true;

                    // Reads the timed messages
                    case "TimedMessages":
                        TimedMessages = ReadTimedMessages(xml);
                        return true;

                    default:
                        return false;
                }
            });
        }

        /// <summary>
        /// Reads a command
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <param name="callback"></param>
        private void ReadCommand<T>(XmlReader xml, Action<T> callback) where T : CommandHandler, new()
        {
            var attributes = xml.HasAttributes;
            var empty = xml.IsEmptyElement;

            // Creates the command
            var command = new T();

            // Reads the keywords
            if (attributes)
            {
                ReadAttributes(xml, (name, value) =>
                {
                    switch (name)
                    {
                        case "keyword":
                            command.KeyWords = value.Split(',');
                            break;
                        case "timeout":
                            command.Timeout = ParseTimeSpan(value);
                            break;
                        case "timeoutCondition":
                            command.TimeoutCondition = value;
                            break;
                    }
                });
            }


            // Reads
            if (!empty)
            {
                callback(command);
            }

            // Checks if the key words are set
            if (command.KeyWords != null && command.KeyWords.Length > 0)
            {
                MessageHandler.Add(command);
            }
        }

        /// <summary>
        /// Adds a moderator command
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        private void ReadModeratorCommand<T>(XmlReader xml) where T : ModeratorCommand, new()
        {
            var attributes = xml.HasAttributes;
            var empty = xml.IsEmptyElement;

            // Creates the command
            var command = new T();

            // Reads the keywords
            if (attributes)
            {
                ReadAttributes(xml, (name, value) =>
                {
                    switch (name)
                    {
                        case "warning":
                            command.Warning = value.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case "warningremovemessage":
                            command.WarningRemoveMessage = value.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case "timeout":
                            command.Timeout = ParseTimeSpan(value);
                            break;
                    }
                });
            }

            // Reads
            if (!empty)
            {
                ReadElement(xml, (name) => { 
                    switch (name)
                    {
                        case "MessageWarning":
                            command.MessageWarning = ReadMessageLogic(xml);
                            return true;
                        case "MessageTimeout":
                            command.MessageTimeout = ReadMessageLogic(xml);
                            return true;
                        default:
                            return false;
                    }
                });
            }

            // Adds the command
            MessageHandler.Add(command);
        }


        /// <summary>
        /// Reads a message
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private ResponseMessage ReadMessage(XmlReader xml)
        {
            var messages = new List<string>();
            if (ReadRelevant(xml))
            {
                // There is a single text
                if (xml.NodeType == XmlNodeType.Text)
                {
                    messages.Add(xml.Value.Trim());
                    xml.Read();
                }
                else
                {
                    ReadElement(xml, (name) =>
                    {
                        switch (name)
                        {
                            case "Text":
                                messages.Add(ReadString(xml).Trim());
                                return true;
                            default:
                                return false;
                        }
                    });
                }
            }
            return new ResponseMessage(messages.ToArray());
        }

        /// <summary>
        /// Reads a message with logic
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private ResponseMessageLogic ReadMessageLogic(XmlReader xml)
        {
            var elements = new List<ResponseMessageLogicElement>();
            if (ReadRelevant(xml))
            {
                // There is a single text
                if (xml.NodeType == XmlNodeType.Text)
                {
                    elements.Add(new ResponseMessageLogicElement(new ResponseMessage(xml.Value.Trim())));
                    xml.Read();
                }
                else
                {
                    var legacyTexts = new List<string>();
                    ReadElement(xml, (name) =>
                    {
                        switch (name)
                        {
                            case "Text":
                                legacyTexts.Add(ReadString(xml).Trim());
                                return true;
                            case "Condition":
                                string condition = null;
                                var attributes = xml.HasAttributes;

                                // Reads the condition
                                if (attributes)
                                {
                                    ReadAttributes(xml, (name, value) =>
                                    {
                                        switch (name)
                                        {
                                            case "if":
                                                condition = value;
                                                break;
                                        }
                                    });
                                }
                                var message = ReadMessage(xml);
                                elements.Add(new ResponseMessageLogicElement(message, condition));
                                return true;
                            default:
                                return false;
                        }
                    });

                    // Adds the elements by the legacy texts
                    if (legacyTexts.Count > 0)
                    {
                        elements.Add(new ResponseMessageLogicElement(new ResponseMessage(legacyTexts.ToArray())));
                    }
                }
            }
            return new ResponseMessageLogic(elements.ToArray());
        }

        /// <summary>
        /// Reads the timed messages
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private TimedMessages ReadTimedMessages(XmlReader xml)
        {
            var attributes = xml.HasAttributes;
            var empty = xml.IsEmptyElement;

            var timedMessages = new List<ResponseMessageLogic>();
            var interval = new TimeSpan();

            // Reads the keywords
            if (attributes)
            {
                ReadAttributes(xml, (name, value) =>
                {
                    switch (name)
                    {
                        case "interval":
                            interval = ParseTimeSpan(value);
                            break;
                    }
                });
            }

            if (!empty)
            {
                ReadElement(xml, (name) => { 
                    switch(name)
                    {
                        case "Message":
                            timedMessages.Add(ReadMessageLogic(xml));
                            return true;
                        default:
                            return false;
                    }
                });
            }

            // Returns the message
            return new TimedMessages(interval, timedMessages.ToArray());
        }

        /// <summary>
        /// Parses the time span
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static TimeSpan ParseTimeSpan(string text)
        {
            // Use the seconds
            if (int.TryParse(text, out var seconds))
                return TimeSpan.FromSeconds(seconds);

            return TimeSpan.ParseExact(text, @"m\:ss", CultureInfo.InvariantCulture);
        }

        #endregion Read

        #region Write

        /// <summary>
        /// Writes the settings
        /// </summary>
        /// <param name="xml"></param>
        protected override void Write(XmlWriter xml)
        {
            foreach (var handler in MessageHandler)
            {
                // Writes a simple command
                if (handler is Command command)
                {
                    WriteCommand(xml, command, "Command", () =>
                    {
                        WriteMessage(xml, command.Message);
                    });
                }
                // Writes the quote command
                else if (handler is QuoteCommand quoteCommand)
                {
                    WriteCommand(xml, quoteCommand, "QuoteCommand", () =>
                    {
                        WriteMessage(xml, "MessageSuccess", quoteCommand.MessageSuccess);
                        WriteMessage(xml, "MessageNotAllowed", quoteCommand.MessageNotAllowed);
                    });
                }
                // Writes the game command
                else if (handler is GameCommand gameCommand)
                {
                    WriteCommand(xml, gameCommand, "GameCommand", () =>
                    {
                        WriteMessage(xml, "MessageSuccess", gameCommand.MessageSuccess);
                        WriteMessage(xml, "MessageNotAllowed", gameCommand.MessageNotAllowed);
                    });
                }
                // Writes the allow command
                else if (handler is AllowCommand allowCommand)
                {
                    WriteCommand(xml, allowCommand, "AllowCommand", () =>
                    {
                        WriteMessage(xml, "MessageSuccess", allowCommand.MessageSuccess);
                    });
                }
                // Writes the link moderator
                else if (handler is LinkModerator linkModerator)
                {
                    WriteModeratorCommand(xml, linkModerator, "LinkModerator");
                }
            }

            // Writes the new subscriber message
            WriteMessage(xml, "NewSubscriber", MessageNewSubscriber);

            // Writes the gifted subscriber message
            WriteMessage(xml, "GiftedSubscriber", MessageGiftedSubscriber);

            // Writes the new follower message
            WriteMessage(xml, "NewFollower", MessageNewFollower);

            // Writes the raid message
            WriteMessage(xml, "Raid", MessageRaid);

            // Writes the timed messages
            WriteMessage(xml, "TimedMessages", TimedMessages);
        }

        /// <summary>
        /// Writes a command
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        private void WriteCommand<T>(XmlWriter xml, T command, string name, Action callback) where T : CommandHandler
        {
            // Writes the header
            xml.WriteStartElement(name);
            xml.WriteAttributeString("keyword", string.Join(',', command.KeyWords));
            if (command.Timeout.TotalSeconds > 0)
            {
                xml.WriteAttributeString("timeout", FormatTimeSpan(command.Timeout));
            }
            if (!string.IsNullOrEmpty(command.TimeoutCondition))
            {
                xml.WriteAttributeString("timeoutCondition", command.TimeoutCondition);
            }

            callback();

            xml.WriteEndElement();
        }

        /// <summary>
        /// Writes a command
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        private void WriteModeratorCommand<T>(XmlWriter xml, T command, string name) where T : ModeratorCommand
        {
            // Writes the header
            xml.WriteStartElement(name);

            xml.WriteAttributeString("warning", command.Warning.ToString());
            xml.WriteAttributeString("warningremovemessage", command.WarningRemoveMessage.ToString());
            xml.WriteAttributeString("timeout", FormatTimeSpan(command.Timeout));

            WriteMessage(xml, "MessageWarning", command.MessageWarning);
            WriteMessage(xml, "MessageTimeout", command.MessageTimeout);

            xml.WriteEndElement();
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        private void WriteMessage(XmlWriter xml, string name, ResponseMessage message)
        {
            if (message.Messages != null)
            {
                xml.WriteStartElement(name);
                WriteMessage(xml, message);
                xml.WriteEndElement();
            }
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        private void WriteMessage(XmlWriter xml, string name, ResponseMessageLogic message)
        {
            if (true)
            {
                xml.WriteStartElement(name);
                WriteMessage(xml, message);
                xml.WriteEndElement();
            }
            
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="name"></param>
        /// <param name="messages"></param>
        private void WriteMessage(XmlWriter xml, string name, TimedMessages messages)
        {
            if (messages.Messages != null)
            {
                xml.WriteStartElement(name);
                xml.WriteAttributeString("interval", FormatTimeSpan(messages.Interval));
                WriteMessage(xml, messages);
                xml.WriteEndElement();
            }
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="message"></param>
        private void WriteMessage(XmlWriter xml, ResponseMessage message)
        {
            if (message.Messages != null)
            {
                // Simple element
                if (message.Messages.Length == 1)
                {
                    xml.WriteString(message.Messages[0]);
                }
                else
                {
                    foreach (var text in message.Messages)
                    {
                        xml.WriteElementString("Text", text);
                    }
                }
            }
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="message"></param>
        private void WriteMessage(XmlWriter xml, ResponseMessageLogic message)
        {
            if (message.Messages != null)
            {
                // Simple element
                if (message.Messages.Length == 1 && string.IsNullOrEmpty(message.Messages[0].Condition))
                {
                    WriteMessage(xml, message.Messages[0].Message);
                }
                else
                {
                    foreach (var element in message.Messages)
                    {
                        xml.WriteStartElement("Condition");
                        if (!string.IsNullOrEmpty(element.Condition))
                        {
                            xml.WriteAttributeString("if", element.Condition);
                        }
                        WriteMessage(xml, element.Message);
                        xml.WriteEndElement();
                    }
                }
            }
        }

        /// <summary>
        /// Writes a message
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="messages"></param>
        private void WriteMessage(XmlWriter xml, TimedMessages messages)
        {
            if (messages.Messages != null)
            {
                foreach (var message in messages.Messages)
                {
                    xml.WriteStartElement("Message");
                    WriteMessage(xml, message);
                    xml.WriteEndElement();
                }
            }
        }

        /// <summary>
        /// Formats the time span
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            return timeSpan.ToString("m:ss", CultureInfo.InvariantCulture);
        }

        #endregion Write
    }
}
