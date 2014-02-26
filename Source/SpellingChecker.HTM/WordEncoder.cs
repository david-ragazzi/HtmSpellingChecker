using System;
using System.Collections.Generic;
using CLA;
using SpellingChecker.Common;

namespace SpellingChecker.HTM
{
    // Words should be encoded to map of bits which are readable for HTM's
    // This encoder works as follow:
    //   1. Hashes the word to a numerical representation;
    //   2. Arrange each character to a respective kth position in Y axis.
    //      I.e. the second char ('5') ocupies a bit in 5th position.
    // Example:
    //   Word: 'dog'
    //   Hash: 15081376
    //   Map of bits:
    //     00100000
    //     10001000
    //     00000000
    //     00000100
    //     00000000
    //     01000000
    //     00000001
    //     00000010
    //     00010000
    //     00000000
    public class WordEncoder : IEncoder
    {
    	public bool UpdateDictionary;
    	public static int MaxHashSize = uint.MaxValue.ToString().Length;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WordEncoder()
        {
        }

        /// <summary>
        /// Transform a word to map of bits and return this.
        /// Words should be encoded to map of bits which are readable for HTMs.
        /// This method works as follow:
        ///   1. Hashes the word to a numerical representation;
        ///   2. Add the word and its hash code to the lookup table for future search;
        ///   3. Arrange each character in hash code to a respective kth position in Y axis.
        ///      I.e. a '5' character in hash code will ocupy a bit in 5th position in Y axis.
        /// Example:
        ///   Word: 'dog'
        ///   Hash: 95081376
        ///   Map of bits:
        ///     00100000 => __0_____
        ///     00001000
        ///     00000000
        ///     00000100
        ///     00000000
        ///     01000000 => _5______
        ///     00000001
        ///     00000010
        ///     00010000
        ///     10000000 => 9_______
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public int[,,] EncodeToHtm(object rawData)
        {
            var word = (string)rawData;

            // Get an hash representation to the word
            uint hashCode = Dictionary.Instance.GetHashFromString(word);
            if (this.UpdateDictionary)
            {
            	Dictionary.Instance.AddWord(word, hashCode);
            }

            // Create a map of bits to this word
            var htmData = new int[MaxHashSize,10,1];

            // Arrange each character to a respective kth position in Y axis
            string hashString = Convert.ToString(hashCode);
            hashString = new String('0', MaxHashSize - hashString.Length) + hashString;
            for (int c = 0; c < MaxHashSize; c++)
            {
                int kthPosition = Convert.ToInt32(char.ConvertFromUtf32(hashString[c]));
                htmData[c, kthPosition, 0] = 1;
            }

            // Arrange each character from a respective kth position in Y axis.
            //TODO: Remove:
			/*string str = Environment.NewLine + "Word:" + word + Environment.NewLine + "<bitmap>" + Environment.NewLine;
            for (int kth = 0; kth < 10; kth++)
            {
                for (int c = 0; c < MaxHashSize; c++)
                {
                    str += Convert.ToString(htmData[c, kth, 0]);
                }
                str += Environment.NewLine;
            }
            str += "</bitmap>";
            Status.Update(str);*/

            return htmData;
        }

        /// <summary>
        /// Transform a map of bits to word and return this.
        /// Words should be decoded from map of bits returned by HTMs.
        /// This method works as follow:
        ///   1. Deciphers each character through its respective kth position in Y axis.
        ///      I.e. a bit ocupping the 5th position in Y axis means '5' character.
        ///   2. Concatenate all characters to hash code.
        ///   2. Search the word in lookup table through its hash code;
        /// Example:
        ///   Word: 'dog'
        ///   Hash: 15081376
        ///   Map of bits:
        ///     00100000 => __0_____
        ///     00001000
        ///     00000000
        ///     00000100
        ///     00000000
        ///     01000000 => _5______
        ///     00000001
        ///     00000010
        ///     00010000
        ///     10000000 => 9_______
        /// </summary>
        /// <param name="htmData"></param>
        /// <returns></returns>
        public object DecodeFromHtm(float[,,] htmData)
        {
        	// Arrange each character from a respective kth position in Y axis.
            string hashString = "";
            for (int c = 0; c < MaxHashSize; c++)
            {
                int kthPosition = -1;
                float bestActivity = 0.0f;
                for (int kth = 0; kth < 10; kth++)
                {
                    float activity = htmData[c, kth, 0];
                    if (activity > bestActivity)
                    {
                        bestActivity = activity;
                        kthPosition = kth;
                    }
                }

                // If found any bit in Y axis
                if (kthPosition >= 0)
                {
                    hashString += kthPosition;
                }
                else
                {
                    // If not found any bit in Y axis then we do not have a valid word
                    hashString = "";
                    break;
                }
            }

            // Get the word from hash representation
            string word = "";
            if (hashString != "")
            {
            	uint hashCode;// = Convert.ToUInt32(hashString);
            	uint.TryParse(hashString, out hashCode);
                word = Dictionary.Instance.RecoverWord(hashCode);
            }

            return word;
        }
    }
}
