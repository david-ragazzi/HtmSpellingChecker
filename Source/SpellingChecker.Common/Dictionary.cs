using System;
using System.Collections.Generic;
using System.Linq;

namespace SpellingChecker.Common
{
	/// <summary>
	/// Description of Dictionary.
	/// </summary>
	public class Dictionary
	{
	    /// <summary>
	    /// A class to store words and their degree of similarity with a mispelled word
	    /// </summary>
	    private class SimilarWord
	    {
	        public string Word = "";
	        public float Similarity;
	    }
	    
		public Dictionary<uint, string> Entries = new Dictionary<uint, string>();

		/// <summary>
		/// Singleton.
		/// </summary>
		public static Dictionary Instance
		{
			get 
			{
				if (_instance == null)
				{
					_instance = new Dictionary();
				}
				return _instance;
			}
		}
		private static Dictionary _instance;
		
		public Dictionary()
		{
		}

        /// <summary>
        /// Adds a word to the lookup table to future search.
        /// Hashing methods are irreversible, so the only way to know which exact word
        /// the HTM returned, is searching by its hash code. 
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public void AddWord(string word, uint hashCode)
        {
            if (!this.Entries.ContainsValue(word))
            {
                this.Entries.Add(hashCode, word);
            }
        }

        /// <summary>
        /// Verify if a word exists in the lookup table.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool ContainsWord(string word)
        {
        	bool entryFound = this.Entries.ContainsValue(word);

            return entryFound;
        }

        /// <summary>
        /// Search for a word in lookup table through its hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        public string RecoverWord(uint hashCode)
        {
            string word = "";
            if (this.Entries.ContainsKey(hashCode))
            {
                word = this.Entries[hashCode];
            }

            return word;
        }

        /// <summary>
        /// This method transforms a word to a number using Dan Bernstein's algorithm
        /// with k=33.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public uint GetHashFromString(string str)
        {
            uint hash = 0;
            for (int c = 0; c < str.Length; c++)
            {
                hash = (hash * 33) + str[c];
            }

            return hash;
        }
		
        /// <summary>
        /// Get all words that are similar to a given word.
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public List<string> GetSimilarWords(string word)
        {
            var similarWords = new List<SimilarWord>();

            // Transverse dictionary looking for similar words
            foreach (var dictWord in Entries.Values.ToList())
            {
                float similarity = Word.Compare(word, dictWord);
                if (similarity > 0.0f && dictWord != word)
                {
                    var similarWord = new SimilarWord();
                    similarWord.Word = dictWord;
                    similarWord.Similarity = similarity;
                    similarWords.Add(similarWord);
                }
            }

            // Ranking by similarity
            similarWords = similarWords.OrderByDescending(w => w.Similarity).ToList();

            return similarWords.Select(w => w.Word).ToList();
        }
	}
}
