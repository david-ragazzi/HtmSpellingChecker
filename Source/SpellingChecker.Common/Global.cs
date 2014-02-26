using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SpellingChecker.Common
{
    /// <summary>
    /// Enum to specify if an found issue is an error (word not found)
    /// or a warning (word found but without sense in context)
    /// </summary>
    public enum IssueType
    {
        All,
        Error,
        Warning
    }
    
    /// <summary>
    /// Enum to specify the type of edit that induced error.
    /// </summary>
    public enum EditType
	{
		Insertion,
		Deletion,
		Substitution,
		Transposition
	}		

    /// <summary>
    /// A class to store details of a issue as well as suggestions of corrections
    /// </summary>
    public class Issue
    {
        public IssueType Type;
        public int Position;
        public string CurrentWord = "";
        public List<string> SuggestedWords = new List<string>();
    }

	public class Utils
	{
		/// <summary>
		/// Splits a corpus to sentences.
		/// </summary>
		/// <param name="corpus"></param>
		/// <returns></returns>
		public static List<string> SplitToSentences(string text)
		{
			var sentences = new List<string>();

			int spacesCount = 0;
			string tempSentence = "";
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (c == ' ')
				{
					spacesCount += 1;
				}
				if (c == '\n')
				{
					// Only add sentences with 2 or more words
					if (spacesCount > 1)
					{
						sentences.Add(tempSentence);
					}
					
					tempSentence = "";
					spacesCount = 0;
				}
				else
				{				
					tempSentence += c;
				}
			}
			
			return sentences;
		}
	}

	/// <summary>
	/// A class that contains functions related to words handling.
	/// </summary>
	public class Word
    {
        /// <summary>
        /// Clean an word by removing accents and any modified characters (á => a, ç => c, etc)
        /// </summary>
        /// <param name="str"></param>
        private static void Clean(ref string str)
        {
            // Converts to lower case.
            str = str.ToLowerInvariant();

            // Remove accents and any modified characters (á => a, ç => c, etc)
            byte[] bytes = System.Text.Encoding.GetEncoding("iso-8859-8").GetBytes(str);
            str = System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// This method uses an edit-distance string matching algorithm: Levenshtein. 
        /// The string edit distance is the total cost of transforming one string into 
        /// another using a set of edit rules, each of which has an associated cost. 
        /// Levenshtein distance is obtained by finding the cheapest way to transform one
        /// string into another. Transformations are the one-step operations of 
        /// (single-phone) insertion, deletion and substitution. In the simplest version 
        /// substitutions cost about two units except when the source and target are 
        /// identical, in which case the cost is zero. Insertions and deletions costs half
        /// that of substitutions.
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        private static int ComputeDistance(string str1, string str2)
        {
            var distance = new int[str1.Length + 1,str2.Length + 1];

            // Initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= str1.Length; distance[i, 0] = i++)
            {
            }
            for (int j = 1; j <= str2.Length; distance[0, j] = j++)
            {
            }

            // Find min distance
            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    int cost = (str2[j - 1] == str1[i - 1])
                                   ? 0
                                   : 1;
                    int min1 = distance[i - 1, j] + 1;
                    int min2 = distance[i, j - 1] + 1;
                    int min3 = distance[i - 1, j - 1] + cost;
                    distance[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }

            return distance[str1.Length, str2.Length];
        }

        /// <summary>
        /// Check if two words have characters in common and returns the degree of similarity.
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static float Compare(string str1, string str2)
        {
            // First remove accent, uppercase, etc.
            Clean(ref str1);
            Clean(ref str2);

            // If tolerance is 2, then two strings are similar if only 2 or less characters are different.
			int tolerance = 1;
            if (str1.Length <= tolerance || str2.Length <= tolerance)
            {
                tolerance = 1;
            }

            // Number of characters not shared by the two words.
            int distance = ComputeDistance(str1, str2);

            // Check if minimum distance is less or equal than tolerance, then the words are similar.
            if (distance <= tolerance)
            {
                float maxLen = Math.Max(str1.Length, str2.Length);
                return 1.0F - distance / maxLen;
            }
            else
            {
                return 0.0F;
            }
        }
        
        /// <summary>
        /// Check if two words have characters in common.
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>        
        public static bool IsSimilar(string str1, string str2)
        {
			return Compare(str1, str2) > 0.0f;
        }
    }

	/// <summary>
	/// A class that contains functions related to GUI status.
	/// </summary>
	public class Status
	{
		public static TextBox StatusLabel;

		/// <summary>
		/// Update the status control with the specified text.
		/// </summary>
		/// <param name="text">Text.</param>
		public static void Update(string text)
		{
			StatusLabel.Text += text + Environment.NewLine;
			Debug.Print(text);
			Application.DoEvents();
		}
	}
}
