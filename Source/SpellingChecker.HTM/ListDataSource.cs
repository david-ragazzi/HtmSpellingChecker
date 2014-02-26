using System;
using System.Collections.Generic;
using CLA;

namespace SpellingChecker.HTM
{
    public class ListDataSource : IDataSource
    {
        /// <summary>
        /// List to be handled.
        /// </summary>
        public List<Object> List;

        /// <summary>
        /// Current position of the list.
        /// </summary>
        public int Position;

        /// <summary>
        /// Constructor
        /// </summary>
        public ListDataSource(List<Object> list)
        {
            // Set fields
            this.List = list;
        }

        /// <summary>
        /// Place cursor on the first element.
        /// </summary>
        public void Initialize()
        {
            // Places the cursor on the first element
            this.Position = 0;
        }

        /// <summary>
        /// Get the next element from list.
        /// If list end is reached then start reading from scratch again.
        /// </summary>
        public object GetNextRecord()
        {
            // If end of list was reached then place cursor on the first element again
            if (this.Position == this.List.Count)
            {
                this.Position = 0;
            }

            // Start reading from last position
            object obj = this.List[this.Position];

            // Increase position of list
            this.Position++;

            return obj;
        }
    }
}
