using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace TwitchBot
{
    /// <summary>
    /// DS 2021-01-30: The quote database
    /// </summary>
    public class Quotes : XmlFile, IEnumerable<Quote>
    {
        /// <summary>
        /// Creates the quote database
        /// </summary>
        public Quotes() : base("Quotes")
        {
        }

        /// <summary>
        /// Gets the list of quotes
        /// </summary>
        private List<Quote> m_Quotes = new List<Quote>();

        /// <summary>
        /// Gets the number of quotes
        /// </summary>
        public int Count
        {
            get { return m_Quotes.Count; }
        }

        /// <summary>
        /// Adds a quote
        /// </summary>
        /// <param name="item"></param>
        public void Add(Quote item)
        {
            m_Quotes.Add(item);
        }

        /// <summary>
        /// Gets the enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Quote> GetEnumerator()
        {
            return m_Quotes.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Quotes.GetEnumerator();
        }

        /// <summary>
        /// Gets a quote by the index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Quote this[int index]
        {
            get { return m_Quotes[index]; }
        }

        /// <summary>
        /// Reads the quotes
        /// </summary>
        /// <param name="xml"></param>
        protected override void Read(XmlReader xml)
        {
            m_Quotes.Clear();

            // Reads all quotes
            ReadElement(xml, (name) =>
            {
                if (name == "Quote")
                {
                    if (ReadRelevant(xml))
                    {
                        var quote = new Quote();
                        ReadElement(xml, (n) =>
                        {
                            switch (n)
                            {
                                case "Text":
                                    quote.Text = ReadString(xml);
                                    return true;

                                case "Author":
                                    quote.Author = ReadString(xml);
                                    return true;

                                case "CreatedBy":
                                    quote.CreatedBy = ReadString(xml);
                                    return true;

                                case "Date":
                                    var date = ReadString(xml);
                                    quote.Date = DateTime.Parse(date);
                                    return true;

                                default:
                                    return false;
                            }
                        });
                        m_Quotes.Add(quote);
                        return true;
                    }
                }
                return false;
            });
        }

        /// <summary>
        /// Writes the quotes
        /// </summary>
        /// <param name="xml"></param>
        protected override void Write(XmlWriter xml)
        {
            foreach (var quote in m_Quotes)
            {
                xml.WriteStartElement("Quote");
                xml.WriteElementString("Text", quote.Text);
                xml.WriteElementString("Author", quote.Author);
                xml.WriteElementString("CreatedBy", quote.CreatedBy);
                xml.WriteElementString("Date", quote.Date.ToString("yyyy-MM-dd"));
                xml.WriteEndElement();
            }
        }

        
    }

    /// <summary>
    /// DS 2021-01-30: A quote
    /// </summary>
    public class Quote
    {
        /// <summary>
        /// Gets and sets the text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets and sets the author of the quote
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets and sets the user that created this quote
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets and sets the date
        /// </summary>
        public DateTime Date { get; set; }
    }
}
