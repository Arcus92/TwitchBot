using System;
using System.Collections.Generic;

namespace TwitchBot
{
    /// <summary>
    /// DS 2021-02-14: A list of temporary items. An item is removed after a specific time.
    /// </summary>
    public class TimedList<T>
    {
        /// <summary>
        /// Gets and sets the interval for a item
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// The dictionary of all allowed users with the expiry date of the entry
        /// </summary>
        private Dictionary<T, DateTime> m_Items = new Dictionary<T, DateTime>();

        /// <summary>
        /// Creates the timed list
        /// </summary>
        /// <param name="interval"></param>
        public TimedList(TimeSpan interval)
        {
            Interval = interval;
        }

        /// <summary>
        /// Adds an item
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            Add(item, Interval);
        }

        /// <summary>
        /// Adds an item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="interval"></param>
        public void Add(T item, TimeSpan interval)
        {
            // Remove the entry
            m_Items.Remove(item);

            // Adds the new entry
            m_Items.Add(item, DateTime.Now + interval);
        }

        /// <summary>
        /// Returns if this item is on the list
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            if (m_Items.TryGetValue(item, out var time))
            {
                if (time > DateTime.Now)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
