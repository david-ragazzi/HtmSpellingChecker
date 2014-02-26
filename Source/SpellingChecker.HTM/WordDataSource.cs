using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Region = CLA.Region;
using CLA;

namespace HtmSpellingChecker
{
    public class WordDataSource : IDataSource
    {
		/// <summary>
        /// Words list to be handled.
		/// </summary>
        private List<String> _words;

        /// <summary>
        /// Current position of the words list.
        /// </summary>
        private int _position;

		/// <summary>
		/// Constructor
		/// </summary>
        public WordDataSource(List<String> words)
		{
			// Set fields
            this._words = words;
		}

        /// <summary>
        /// Singleton pattern.
        /// </summary>
        public static WordDataSource Instance
        {
            get 
            {
                if (instance == null)
                {
                    instance = new WordDataSource(null);
                }
                return instance;
            }
            set
            {
            	instance = value;
            }
        }
        private static WordDataSource instance;

		/// <summary>
		/// Place cursor on the first element.
		/// </summary>
		public void Initialize()
		{
			// Places the cursor on the first element
			this._position = 0;
		}

		/// <summary>
		/// Get the next element from list.
		/// If list end is reached then start reading from scratch again.
		/// </summary>
		public object GetNextRecord()
		{
			// If end of list was reached then place cursor on the first element again
			if (this._position == this._words.Count)
			{
                this._position = 0;
			}

			// Start reading from last position
            string word = this._words[this._position];

            // Increase position of list
            this._position++;
            
            return word;
		}
    }
}
