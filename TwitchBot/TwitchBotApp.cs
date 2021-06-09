using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using TwitchBot.Messages;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.FollowerService;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchBot
{
    /// <summary>
    /// DS 2021-01-30: The main application
    /// </summary>
    public class TwitchBotApp
    {
        /// <summary>
        /// Runs the bot
        /// </summary>
        public void Run()
        {
            // Loads the settings
            LoadSettings();

            // Loads the commands
            LoadCommands();

            // Loads the quotes
            LoadQuotes();

            // Runs the setup
            if (string.IsNullOrEmpty(m_Settings.OAuthTokenChannel) || string.IsNullOrEmpty(m_Settings.OAuthTokenBot) || string.IsNullOrEmpty(m_Settings.ClientID))
            {
                // Runs the setup
                Setup();
                Console.Clear();
            }

            Loop();
        }

        #region Bot

        /// <summary>
        /// The twitch client
        /// </summary>
        private TwitchClient m_Client;

        /// <summary>
        /// The twitch api
        /// </summary>
        private TwitchAPI m_Api;

        /// <summary>
        /// The follower service
        /// </summary>
        private FollowerService m_FollowerService;

        /// <summary>
        /// Gets the channel
        /// </summary>
        public string Channel
        {
            get { return m_Settings.Channel; }
        }

        /// <summary>
        /// The broadcaster id
        /// </summary>
        private string m_BroadcasterID;

        /// <summary>
        /// Runs the bot loop
        /// </summary>
        private void Loop()
        {
            Console.WriteLine(" Starting bot...");
            Console.WriteLine(" ------------------------------------------------------------------");
            Console.WriteLine(" Enter 'exit' to disconnect the bot.");

            // Connect the api
            m_Api = new TwitchAPI();
            m_Api.Settings.ClientId = m_Settings.ClientID;
            m_Api.Settings.AccessToken = m_Settings.OAuthTokenChannel;

            // Gets the current broadcaster
            var broadcaster = GetUser(m_Settings.Channel);
            m_BroadcasterID = broadcaster.Id;

            // Connects the follower service
            m_FollowerService = new FollowerService(m_Api);
            var channels = new List<string>();
            channels.Add(m_Settings.Channel);
            m_FollowerService.SetChannelsByName(channels);
            m_FollowerService.OnNewFollowersDetected += OnNewFollowersDetected;
            m_FollowerService.Start();

            // Build the credentials
            ConnectionCredentials credentials = new ConnectionCredentials(m_Settings.Channel, "oauth:" + m_Settings.OAuthTokenBot);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            m_Client = new TwitchClient(customClient);
            m_Client.Initialize(credentials, m_Settings.Channel);

            // Adds the events
            m_Client.OnLog += OnLog;
            m_Client.OnJoinedChannel += OnJoinedChannel;
            m_Client.OnMessageReceived += OnMessageReceived;
            m_Client.OnWhisperReceived += OnWhisperReceived;
            m_Client.OnNewSubscriber += OnNewSubscriber;
            m_Client.OnGiftedSubscription += OnGiftedSubscription;
            m_Client.OnRaidNotification += OnRaidNotification;
            m_Client.OnConnected += OnConnected;
            m_Client.AddChatCommandIdentifier('!');

            // Connect to the server
            Console.WriteLine(" Connecting to chat...");
            m_Client.Connect();

            GetChannelInformation();

            // Wait for exit
            bool loop = true;
            while (loop)
            {
                var line = Console.ReadLine().Trim();
                var command = line;
                var argument = string.Empty;
                var index = line.IndexOf(' ');
                if (index >= 0)
                {
                    command = line.Substring(0, index);
                    argument = line.Substring(index + 1);
                }

                switch (command.ToLowerInvariant())
                {
                    // Reloads the commands
                    case "reload":
                        LoadQuotes();
                        LoadCommands();
                        break;

                    // Test commands
                    case "test":
                        switch (argument.ToLowerInvariant())
                        {
                            case "raid":
                                OnRaidNotification(this, new OnRaidNotificationArgs()
                                {
                                    Channel = m_BroadcasterID,
                                    RaidNotification = new RaidNotification(null, null, null, "Tester", null, "1", "tester", false, "1", "Tester", "tester", null, null, false, null, null, null, false, TwitchLib.Client.Enums.UserType.Viewer, "tester")
                                });
                                break;

                            case "newsubscriber":
                                OnNewSubscriber(this, new OnNewSubscriberArgs()
                                {
                                    Channel = m_BroadcasterID,
                                    Subscriber = new Subscriber(null, null, null, Color.Red, "Tester", null, "1", "tester", null, null, null, null, true, null, null, TwitchLib.Client.Enums.SubscriptionPlan.Prime, "Prime", null, null, false, false, true, false, null, TwitchLib.Client.Enums.UserType.Viewer, null, m_BroadcasterID)
                                });
                                break;

                            case "giftedsubscriber":
                                OnGiftedSubscription(this, new OnGiftedSubscriptionArgs()
                                {
                                    Channel = m_BroadcasterID,
                                    GiftedSubscription = new GiftedSubscription(null, null, null, "Tester", null, "1", "tester", false, null, null, null, null, null, null, null, TwitchLib.Client.Enums.SubscriptionPlan.Prime, null, true, null, null, null, false, TwitchLib.Client.Enums.UserType.Viewer, null)
                                });
                                break;
                        }
                        break;

                    // End the application 
                    case "exit":
                    case "quit":
                        loop = false;
                        break;
                }
            }

            // Disconnect from the server
            m_Client.Disconnect();
        }



        #region Events

        /// <summary>
        /// A new follower was detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewFollowersDetected(object sender, OnNewFollowersDetectedArgs e)
        {
            foreach (var follower in e.NewFollowers)
            {
                SendMessage(m_Commands.MessageNewFollower, (name, argument) =>
                {
                    switch (name)
                    {
                        // The user name of the sender
                        case "username":
                            return follower.FromUserName;

                        default:
                            return HandleMessageParameter(name, argument);
                    }
                });
            }

        }

        /// <summary>
        /// The raid notification
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            SendMessage(m_Commands.MessageRaid, (name, argument) =>
            {
                switch (name)
                {
                    // The user name of the sender
                    case "username":
                        return e.RaidNotification.DisplayName;

                    default:
                        return HandleMessageParameter(name, argument);
                }
            });
        }

        /// <summary>
        /// A subscription was gifted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            SendMessage(m_Commands.MessageGiftedSubscriber, (name, argument) =>
            {
                switch (name)
                {
                    // The user name of the raid
                    case "username":
                        return e.GiftedSubscription.DisplayName;

                    // The recipient
                    case "recipient":
                        return e.GiftedSubscription.MsgParamRecipientDisplayName;

                    // The months
                    case "months":
                        return e.GiftedSubscription.MsgParamMonths;

                    // The months
                    case "streak":
                        return e.GiftedSubscription.MsgParamMultiMonthGiftDuration;

                    // The plan name
                    case "plan":
                        return e.GiftedSubscription.MsgParamSubPlanName;

                    default:
                        return HandleMessageParameter(name, argument);
                }
            });
        }

        /// <summary>
        /// A new subscriber
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            SendMessage(m_Commands.MessageNewSubscriber, (name, argument) =>
            {
                switch (name)
                {
                    // The user name of the sender
                    case "username":
                        return e.Subscriber.DisplayName;

                    // The months
                    case "months":
                        return e.Subscriber.MsgParamCumulativeMonths;

                    // The months
                    case "streak":
                        return e.Subscriber.MsgParamStreakMonths;

                    // The plan name
                    case "plan":
                        return e.Subscriber.SubscriptionPlanName;

                    default:
                        return HandleMessageParameter(name, argument);
                }
            });
        }

        /// <summary>
        /// The bot is connected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($" Connected to {e.BotUsername}!");
        }

        /// <summary>
        /// The bot joined the channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine(" Channel joined!");
        }

        /// <summary>
        /// A message was received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.IsMe)
                return;

            // Handles the message
            HandleMessage(e.ChatMessage);
        }

        /// <summary>
        /// A whisper was received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
        }

        /// <summary>
        /// The log event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLog(object sender, OnLogArgs e)
        {
            // Console.WriteLine($"{e.BotUsername}: {e.Data}");
        }

        #endregion Events

        #region Messages

        /// <summary>
        /// Handles the default message parameter
        /// </summary>
        /// <param name="name"></param>
        /// <param name="argument"></param>
        /// <returns></returns>
        public string HandleMessageParameter(string name, string argument)
        {
            switch (name)
            {
                // Returns a random number
                case "random":
                    int max = 6;
                    if (argument != null && int.TryParse(argument, out var tmp))
                    {
                        if (tmp > 0)
                            max = tmp;
                    }
                    var r = GetRandomNumber(max) + 1;
                    return r.ToString();

                // Returns a random user
                case "randomuser":
                    var type = ParseChatterSelectType(argument);
                    var chatter = GetRandomChatter(type, null);
                    return chatter.Username;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns a message parameter handler for the given message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public OnResponseMessageParameterHandler HandleMessageParameter(ChatMessage message, string arguments)
        {
            return (name, argument) =>
            {
                // Handles the parameter
                switch (name)
                {
                    // The user name of the sender
                    case "username":
                        return message.Username;

                    // Is this user the broadcaster
                    case "isbroadcaster":
                        return message.IsBroadcaster.ToString();

                    // Is this user a subscriber
                    case "issubscriber":
                        return message.IsSubscriber.ToString();

                    // Is this user a partner
                    case "ispartner":
                        return message.IsPartner.ToString();

                    // Is this user a staff member
                    case "isstaff":
                        return message.IsStaff.ToString();

                    // Is this user a vip
                    case "isvip":
                        return message.IsVip.ToString();

                    // Is this user a moderator
                    case "ismoderator":
                        return message.IsModerator.ToString();

                    // The number of bits
                    case "bits":
                        return message.Bits.ToString();

                    // The number of subscribed months
                    case "subscribedmonths":
                        return message.SubscribedMonthCount.ToString();

                    // The text after the command
                    case "argument":
                        return arguments;
                    // Returns a random user
                    case "randomuser":
                        var type = ParseChatterSelectType(argument);
                        var chatter = GetRandomChatter(type, message.Username);
                        return chatter.Username;

                    default:
                        return HandleMessageParameter(name, argument);
                }
            };
        }

        /// <summary>
        /// Handles the message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool HandleMessage(ChatMessage message)
        {
            // Keep track of the active chatters
            ActiveChatters.Add(message.Username);

            foreach (var handler in m_Commands.MessageHandler)
            {
                if (handler.Handle(this, message))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sends the message
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            m_Client.SendMessage(m_Settings.Channel, message);
        }

        /// <summary>
        /// Times the given user out
        /// </summary>
        /// <param name="username"></param>
        /// <param name="timeout"></param>
        /// <param name="message"></param>
        public void TimeoutUser(string username, TimeSpan timeout, string message = "")
        {
            m_Client.TimeoutUser(Channel, username, timeout, message);
        }

        /// <summary>
        /// Deletes the message
        /// </summary>
        /// <param name="message"></param>
        public void DeleteMessage(ChatMessage message)
        {
            m_Client.DeleteMessage(message.Channel, message);
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="response"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SendMessage(IResponseMessage response, ChatMessage message, string arguments)
        {
            // Gets the text of the message
            return SendMessage(response, HandleMessageParameter(message, arguments));
        }

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <param name="response"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public bool SendMessage(IResponseMessage response, OnResponseMessageParameterHandler handler = null)
        {
            // Gets the text of the message
            var text = response.GetText(handler == null ? HandleMessageParameter : handler);

            // Prints the message
            if (!string.IsNullOrEmpty(text))
            {
                SendMessage(text);
                return true;
            }

            return false;
        }

        #endregion Messages

        #region Timed messages

        /// <summary>
        /// The timer
        /// </summary>
        private Timer m_Timer;

        /// <summary>
        /// The index of the next timed message
        /// </summary>
        private int m_TimedMessageIndex;

        /// <summary>
        /// Starts the timer
        /// </summary>
        private void StartTimer()
        {
            StopTimer();

            // Do not active the timer if there are no timed messages
            if (m_Commands.TimedMessages.Messages == null || m_Commands.TimedMessages.Messages.Length == 0)
                return;

            // No interval
            if (m_Commands.TimedMessages.Interval.TotalMilliseconds == 0)
                return;

            // Use a random start index
            m_TimedMessageIndex = GetRandomNumber(m_Commands.TimedMessages.Messages.Length);

            // Creates the timer
            m_Timer = new Timer();
            m_Timer.Interval = m_Commands.TimedMessages.Interval.TotalMilliseconds;
            m_Timer.Elapsed += OnTimerElapsed;
            m_Timer.Start();
        }

        /// <summary>
        /// The timer event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Ignore empty messages
            var messages = m_Commands.TimedMessages.Messages;
            if (messages == null || messages.Length == 0)
                return;

            // Reset the index
            if (m_TimedMessageIndex >= messages.Length)
                m_TimedMessageIndex = 0;

            // Sends the message
            var message = messages[m_TimedMessageIndex];
            SendMessage(message, HandleMessageParameter);

            m_TimedMessageIndex++;
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        #endregion Timed messages

        #region Chatters

        /// <summary>
        /// The last chatters update
        /// </summary>
        private DateTime m_ChattersUpdate;

        /// <summary>
        /// The chatters update interval
        /// </summary>
        private TimeSpan m_ChattersUpdateInterval = new TimeSpan(0, 1, 0);

        /// <summary>
        /// The list of chatters
        /// </summary>
        private List<ChatterFormatted> m_Chatters;

        /// <summary>
        /// Gets the list of all active chatters
        /// </summary>
        public TimedList<string> ActiveChatters { get; } = new TimedList<string>(TimeSpan.FromMinutes(30));

        /// <summary>
        /// The select type for the random chatter selection
        /// </summary>
        [Flags]
        public enum ChatterSelectType
        {
            All = 0,
            ExcludeMe = 1,
            ExcludeStreamer = 2,
            OnlyActive = 4,
            ExcludeActive = 8,
            OnlyModerators = 16,
            ExcludeModerators = 32,
        }

        /// <summary>
        /// Parses the chatter selewct type
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ChatterSelectType ParseChatterSelectType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return ChatterSelectType.All;
            if (Enum.TryParse<ChatterSelectType>(name, true, out var type))
            {
                return type;
            }

            return ChatterSelectType.All;
        }

        /// <summary>
        /// Returns a list of chatters
        /// </summary>
        public List<ChatterFormatted> GetChatters()
        {
            // Update is needed
            if (m_Chatters == null || m_ChattersUpdate + m_ChattersUpdateInterval < DateTime.Now)
            {
                try
                {
                    m_Chatters = m_Api.Undocumented.GetChattersAsync(m_Settings.Channel).GetAwaiter().GetResult();
                    m_ChattersUpdate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" GetChatters:");
                    Console.WriteLine(ex);
                    if (m_Chatters == null)
                        m_Chatters = new List<ChatterFormatted>();
                }
                
            }

            return m_Chatters;
        }

        /// <summary>
        /// Returns a random chatter
        /// </summary>
        /// <param name="type"></param>
        /// <param name="me"></param>
        /// <returns></returns>
        public ChatterFormatted GetRandomChatter(ChatterSelectType type, string me = null)
        {
            var list = FilterChatter(GetChatters(), type, me).ToList();
            if (list.Count == 0)
                return new ChatterFormatted("???", UserType.Viewer);

            var i = GetRandomNumber(list.Count);
            return list[i];
        }

        /// <summary>
        /// Filters the chatter list
        /// </summary>
        /// <param name="chatter"></param>
        /// <param name="type"></param>
        /// <param name="me"></param>
        /// <returns></returns>
        public IEnumerable<ChatterFormatted> FilterChatter(IEnumerable<ChatterFormatted> chatter, ChatterSelectType type, string me = null)
        {
            // Apply the ExcludeMe filter
            if (type.HasFlag(ChatterSelectType.ExcludeMe))
            {
                chatter = chatter.Where(c => c.Username != me);
            }

            // Apply the ExcludeStreamer filter
            if (type.HasFlag(ChatterSelectType.ExcludeStreamer))
            {
                chatter = chatter.Where(c => c.Username != Channel);
            }

            // Apply the OnlyActive filter
            if (type.HasFlag(ChatterSelectType.OnlyActive))
            {
                chatter = chatter.Where(c => ActiveChatters.Contains(c.Username));
            }

            // Apply the ExcludeActive filter
            if (type.HasFlag(ChatterSelectType.ExcludeActive))
            {
                chatter = chatter.Where(c => !ActiveChatters.Contains(c.Username));
            }

            // Apply the OnlyModerators filter
            if (type.HasFlag(ChatterSelectType.OnlyModerators))
            {
                chatter = chatter.Where(c => c.UserType == UserType.Moderator);
            }

            // Apply the ExcludeActive filter
            if (type.HasFlag(ChatterSelectType.ExcludeModerators))
            {
                chatter = chatter.Where(c => c.UserType != UserType.Moderator);
            }
            return chatter;
        }

        #endregion Chatters

        #region Stream

        /// <summary>
        /// Gets the user
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public User GetUser(string login)
        {
            try
            { 
                var logins = new List<string>();
                logins.Add(login);
                var result = m_Api.Helix.Users.GetUsersAsync(logins: logins).GetAwaiter().GetResult();
                return result.Users.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" GetUser:");
                Console.WriteLine(ex);
            }
            return null;
        }

        /// <summary>
        /// Returns the current channel information
        /// </summary>
        public ChannelInformation GetChannelInformation()
        {
            try
            {
                var result = m_Api.Helix.Channels.GetChannelInformationAsync(m_BroadcasterID).GetAwaiter().GetResult();
                return result.Data.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" GetChannelInformation:");
                Console.WriteLine(ex);
            }
            return null;
        }

        /// <summary>
        /// Sets the channel information
        /// </summary>
        /// <param name="request"></param>
        public void SetChannelInformation(ModifyChannelInformationRequest request)
        {
            try
            {
                m_Api.Helix.Channels.ModifyChannelInformationAsync(m_BroadcasterID, request).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" SetChannelInformation:");
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Returns the game by id
        /// </summary>
        /// <param name="gameID"></param>
        public Game GetGameByID(string gameID)
        {
            try
            {
                var gameIDs = new List<string>();
                gameIDs.Add(gameID);
                var result = m_Api.Helix.Games.GetGamesAsync(gameIds: gameIDs).GetAwaiter().GetResult();
                return result.Games.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" GetGameByID:");
                Console.WriteLine(ex);
            }
            return null;
        }

        /// <summary>
        /// Returns the game by id
        /// </summary>
        /// <param name="gameName"></param>
        public Game GetGameByName(string gameName)
        {
            try
            {
                var gameNames = new List<string>();
                gameNames.Add(gameName);
                var result = m_Api.Helix.Games.GetGamesAsync(gameNames: gameNames).GetAwaiter().GetResult();
                return result.Games.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" GetGameByName:");
                Console.WriteLine(ex);
            }
            return null;
        }

        #endregion Stream

        #endregion Bot

        #region Setup

        /// <summary>
        /// The OAuth port
        /// </summary>
        private const int OAuthPort = 12564;

        /// <summary>
        /// The OAuth scope for the channel
        /// </summary>
        private const string OAuthScopeChannel = "user:edit:broadcast";

        /// <summary>
        /// The OAuth scope for the bot user
        /// </summary>
        private const string OAuthScopeBot = "channel:moderate chat:edit chat:read whispers:read whispers:edit";

        /// <summary>
        /// The OAuth index page
        /// </summary>
        private const string OAuthIndexPage = "" +
            "<html>" +
            "<head></head>" +
            "<body>" +
            "<script>" +
            "if (document.location.hash) document.location = \"/setup?\" + document.location.hash.substr(1);" +
            "</script>" +
            "</body>" +
            "</html>";

        /// <summary>
        /// Runs the setup
        /// </summary>
        private void Setup()
        {
            var redirectUrl = $"http://localhost:{OAuthPort}/";

            // Creates the web server
            var listner = new HttpListener();
            listner.Prefixes.Add(redirectUrl);
            listner.Start();


            Console.WriteLine(" Setup bot");
            Console.WriteLine(" ------------------------------------------------------------------");
            Console.WriteLine(" Step 1: Create you own Twitch app in the Twitch developer console.");
            Console.WriteLine(" This app is used to authorize your bot account to this application.");
            Console.WriteLine(" You can skip this if you have already created an app.");
            Console.WriteLine();
            Console.WriteLine(" Name:         Choose any name you like, eg. 'MyChatBot App'");
            Console.WriteLine(" Redirect url: " + redirectUrl);
            Console.WriteLine(" Category:     Chat Bot");
            Console.WriteLine();

            // Opens the twitch developer console
            Console.WriteLine(" <Press any key to open the Twitch developer console>");
            Console.ReadKey();
            OpenBrowser("https://dev.twitch.tv/console/apps");

            Console.WriteLine();
            Console.WriteLine(" ------------------------------------------------------------------");
            Console.WriteLine(" Step 2: Enter the client id and you app secret.");
            Console.WriteLine();
            Console.Write(" Client id: ");
            var clientID = Console.ReadLine();

            // Opens the OAuth page
            Console.WriteLine();
            Console.WriteLine(" ------------------------------------------------------------------");
            Console.WriteLine(" Step 3: Authorize the channel.");

            var accessTokenChannel = GetAccessTokenFromWebSite(listner, clientID, redirectUrl, OAuthScopeChannel);

            // Opens the OAuth page
            Console.WriteLine();
            Console.WriteLine(" ------------------------------------------------------------------");
            Console.WriteLine(" Step 4: Authorize the bot.");

            var accessTokenBot = GetAccessTokenFromWebSite(listner, clientID, redirectUrl, OAuthScopeBot);

            listner.Stop();

            Console.WriteLine(" ------------------------------------------------------------------");
            Console.Write(" Step 5: Enter you channel name: ");
            var channel = Console.ReadLine();

            // Saves the token and the channel id
            m_Settings.OAuthTokenChannel = accessTokenChannel;
            m_Settings.OAuthTokenBot = accessTokenBot;
            m_Settings.Channel = channel;
            m_Settings.ClientID = clientID;
            SaveSettings();
        }

        /// <summary>
        /// Hosts a website to fetch the access token
        /// </summary>
        /// <param name="listner"></param>
        /// <param name="clientID"></param>
        /// <param name="redirectUrl"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static string GetAccessTokenFromWebSite(HttpListener listner, string clientID, string redirectUrl, string scope)
        {
            var oauthUrl = $"https://id.twitch.tv/oauth2/authorize?client_id={Uri.EscapeUriString(clientID)}&response_type=token&redirect_uri={Uri.EscapeUriString(redirectUrl)}&scope={Uri.EscapeUriString(scope)}";

            Console.WriteLine(" <Press any key to open OAuth page or press 'C' to show the link>");
            var keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.C)
            {
                Console.WriteLine(" ------------------------------------------------------------------");
                Console.WriteLine($" {oauthUrl}");
                Console.WriteLine(" ------------------------------------------------------------------");
            }
            else
            {
                OpenBrowser(oauthUrl);
            }
            Console.WriteLine(" Waiting for authorization...");


            // The access token
            string accessToken = null;

            // The server loop
            bool runServer = true;
            while (runServer)
            {
                var context = listner.GetContext();
                var request = context.Request;
                var response = context.Response;

                switch (request.Url.AbsolutePath)
                {
                    // The main page
                    case "/":
                        SendHttpResponse(response, 200, OAuthIndexPage);
                        break;

                    // The setup page
                    case "/setup":
                        accessToken = request.QueryString["access_token"];
                        if (string.IsNullOrEmpty(accessToken))
                            goto default;

                        // Done! Close the server.
                        SendHttpResponse(response, 200, "Yeah! We got the access token! You can close this window now.");
                        runServer = false;
                        break;


                    // Error page
                    default:
                        SendHttpResponse(response, 404, "NOT FOUND");
                        break;
                }
            }
            return accessToken;
        }

        #endregion Setup

        #region Settings

        /// <summary>
        /// The file name of the settings
        /// </summary>
        private const string SettingsFileName = "Settings.xml";

        /// <summary>
        /// The current loaded settings
        /// </summary>
        private Settings m_Settings = new Settings();

        /// <summary>
        /// Loads the settings
        /// </summary>
        private void LoadSettings()
        {
            // Loads the file
            if (File.Exists(SettingsFileName))
            {
                m_Settings.Open(SettingsFileName);
            }
        }

        /// <summary>
        /// Saves the settings
        /// </summary>
        private void SaveSettings()
        {
            if (m_Settings == null)
                return;

            m_Settings.Save(SettingsFileName);
        }

        #endregion Settings

        #region Commands

        /// <summary>
        /// The file name of the commands
        /// </summary>
        private const string CommandsFileName = "Commands.xml";

        /// <summary>
        /// The current loaded commands
        /// </summary>
        private Commands m_Commands = new Commands();

        /// <summary>
        /// Loads the commands
        /// </summary>
        private void LoadCommands()
        {
            // Stops the timer
            StopTimer();

            // Resets the commands
            m_Commands.Reset();

            // Loads the file
            if (File.Exists(CommandsFileName))
            {
                try
                {
                    m_Commands.Open(CommandsFileName);

                    // Starts the timer
                    StartTimer();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Saves the commands
        /// </summary>
        private void SaveCommands()
        {
            if (m_Commands == null)
                return;

            m_Commands.Save(CommandsFileName);
        }

        #endregion Commands

        #region Moderator

        /// <summary>
        /// Gets the list of all allowed users
        /// </summary>
        public TimedList<string> AllowedUsers { get; } = new TimedList<string>(TimeSpan.FromMinutes(5));


        /// <summary>
        /// Gets the list of all warned users
        /// </summary>
        public TimedList<string> WarnedUsers { get; } = new TimedList<string>(TimeSpan.FromMinutes(5));

        #endregion Moderator

        #region Quotes

        /// <summary>
        /// The file name of the quotes
        /// </summary>
        private const string QuotesFileName = "Quotes.xml";

        /// <summary>
        /// The quotes
        /// </summary>
        private Quotes m_Quotes = new Quotes();

        /// <summary>
        /// Gets the quotes
        /// </summary>
        public Quotes Quotes 
        {
            get { return m_Quotes; } 
        }

        /// <summary>
        /// Loads the quotes
        /// </summary>
        private void LoadQuotes()
        {
            // Loads the file
            if (File.Exists(QuotesFileName))
            {
                m_Quotes.Open(QuotesFileName);
            }
        }

        /// <summary>
        /// Saves the quotes
        /// </summary>
        private void SaveQuotes()
        {
            if (m_Settings == null)
                return;

            m_Quotes.Save(QuotesFileName);
        }

        /// <summary>
        /// Adds a quote
        /// </summary>
        /// <param name="quote"></param>
        public void AddQuote(Quote quote)
        {
            m_Quotes.Add(quote);

            // Saves the quotes
            SaveQuotes();
        }

        #endregion Quotes

        #region Random

        /// <summary>
        /// A random number generator
        /// </summary>
        private static readonly Random Random = new Random();

        /// <summary>
        /// Gets a new random number
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int GetRandomNumber(int max)
        {
            return Random.Next(max);
        }

        /// <summary>
        /// Returns a random text
        /// </summary>
        /// <param name="texts"></param>
        /// <returns></returns>
        public static string GetRandomText(string[] texts)
        {
            if (texts == null || texts.Length == 0)
                return null;

            return texts[GetRandomNumber(texts.Length)];
        }

        #endregion Random

        #region Utils

        /// <summary>
        /// Opens the browser
        /// </summary>
        /// <param name="url"></param>
        private static void OpenBrowser(string url)
        {
            var info = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(info);
        }

        /// <summary>
        /// Sends the response
        /// </summary>
        /// <param name="response"></param>
        /// <param name="statusCode"></param>
        /// <param name="content"></param>
        private static void SendHttpResponse(HttpListenerResponse response, int statusCode, string content)
        {
            // Builds the response
            var data = Encoding.UTF8.GetBytes(content);

            response.ContentLength64 = data.Length;
            response.ContentEncoding = Encoding.UTF8;
            response.StatusCode = statusCode;
            response.OutputStream.Write(data, 0, data.Length);
            response.Close();
        }

        #endregion Utils
    }
}
