using System;
using System.IO;
using System.Xml;

namespace TwitchBot
{
    /// <summary>
    /// DS 2021-01-30: The xml helper
    /// </summary>
    public abstract class XmlFile
    {
        /// <summary>
        /// The root element
        /// </summary>
        public string m_RootElement;

        /// <summary>
        /// Creates the xml file
        /// </summary>
        /// <param name="rootElement"></param>
        public XmlFile(string rootElement)
        {
            m_RootElement = rootElement;
        }

        #region Read & write

        /// <summary>
        /// Opens the settings
        /// </summary>
        /// <param name="file"></param>
        public void Open(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                Open(stream);
            }
        }

        /// <summary>
        /// Opens the settings
        /// </summary>
        /// <param name="stream"></param>
        public void Open(Stream stream)
        {
            using (var xml = XmlReader.Create(stream))
            {
                xml.ReadStartElement(m_RootElement);

                if (ReadRelevant(xml))
                {
                    Read(xml);
                }

                xml.ReadEndElement();
            }
        }

        /// <summary>
        /// Reads the next relevant element
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        protected static bool ReadRelevant(XmlReader xml)
        {
            while (xml.Read())
            {
                if (xml.NodeType != XmlNodeType.Whitespace && 
                    xml.NodeType != XmlNodeType.Comment)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Reads the xml file
        /// </summary>
        /// <param name="xml"></param>
        protected abstract void Read(XmlReader xml);

        /// <summary>
        /// Saves the settings
        /// </summary>
        /// <param name="file"></param>
        public void Save(string file)
        {
            using (var stream = new FileStream(file, FileMode.Create))
            {
                Save(stream);
            }
        }

        /// <summary>
        /// Saves the settings
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            // Creates xml settings
            var xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;

            // Writes the file
            using (var xml = XmlWriter.Create(stream, xmlSettings))
            {
                xml.WriteStartDocument();

                // Writes the settings
                xml.WriteStartElement(m_RootElement);
                Write(xml);
                xml.WriteEndElement();

                xml.WriteEndDocument();
            }
        }

        /// <summary>
        /// Writes the xml file
        /// </summary>
        /// <param name="xml"></param>
        protected abstract void Write(XmlWriter xml);

        #endregion Read & write

        #region Utils

        /// <summary>
        /// Reads an element string
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static string ReadString(XmlReader xml)
        {
            if (xml.IsEmptyElement)
                return null;

            if (!ReadRelevant(xml) || xml.NodeType != XmlNodeType.Text)
                throw new ArgumentException();

            var value = xml.Value;
            xml.Read();
            return value;
        }

        /// <summary>
        /// Reads a new element
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="callback"></param>
        public static void ReadElement(XmlReader xml, Func<string, bool> callback)
        {
            do
            {
                switch (xml.NodeType)
                {
                    // The start of a new element
                    case XmlNodeType.Element:
                        if (!callback.Invoke(xml.Name))
                            xml.Skip();
                        break;

                    // The end of the current element
                    case XmlNodeType.EndElement:
                        return;
                }
            }
            while (ReadRelevant(xml));
        }

        /// <summary>
        /// Reads a new attribute
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="callback"></param>
        public static void ReadAttributes(XmlReader reader, Action<string, string> callback)
        {
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    callback.Invoke(reader.Name, reader.Value);
                }
                while (reader.MoveToNextAttribute());
            }
        }

        #endregion Utils
    }
}
