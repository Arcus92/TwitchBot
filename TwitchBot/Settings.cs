using System.Xml;

namespace TwitchBot
{
    /// <summary>
    /// DS 2021-01-30: The settings of the application
    /// </summary>
    public class Settings : XmlFile
    {
        /// <summary>
        /// Creates the settings file
        /// </summary>
        public Settings() : base("Settings")
        {
        }

        /// <summary>
        /// Gets the channel to join
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Gets the client id
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// Gets the OAuth token of the channel account
        /// </summary>
        public string OAuthTokenChannel { get; set; }

        /// <summary>
        /// Gets the OAuth token of the bot account
        /// </summary>
        public string OAuthTokenBot { get; set; }

        

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
                    case "Channel":
                        Channel = ReadString(xml);
                        return true;

                    case "ClientID":
                        ClientID = ReadString(xml);
                        return true;

                    case "OAuthTokenChannel":
                        OAuthTokenChannel = ReadString(xml);
                        return true;

                    case "OAuthTokenBot":
                        OAuthTokenBot = ReadString(xml);
                        return true;

                    default:
                        return false;
                }
            });
        }

        #endregion Read

        #region Write

        /// <summary>
        /// Writes the settings
        /// </summary>
        /// <param name="xml"></param>
        protected override void Write(XmlWriter xml)
        {
            if (!string.IsNullOrEmpty(Channel)) xml.WriteElementString("Channel", Channel);
            if (!string.IsNullOrEmpty(ClientID)) xml.WriteElementString("ClientID", ClientID);
            if (!string.IsNullOrEmpty(OAuthTokenChannel)) xml.WriteElementString("OAuthTokenChannel", OAuthTokenChannel);
            if (!string.IsNullOrEmpty(OAuthTokenBot)) xml.WriteElementString("OAuthTokenBot", OAuthTokenBot);
        }

        #endregion Write
    }
}
