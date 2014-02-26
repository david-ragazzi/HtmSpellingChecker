using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SpellingChecker.Common;

namespace SpellingChecker.Levenshtein
{
    public class Engine
    {
        /// <summary>
        /// Trains the spelling checker with a corpus.
        /// </summary>
        /// <param name="corpus"></param>
        public void Train(string corpus)
        {
            // Get words from training text
            var tokenizer = new Tokenizer();
            List<Tokenizer.Token> tokens = tokenizer.Tokenize(corpus);

            // Train
			Status.Update("Trainning Levenshtein engine...");
            for (int i = 0; i < tokens.Count; i++)
            {
                string word = tokens[i].Text;

                // Get an hash representation to the word
                uint hashCode = Dictionary.Instance.GetHashFromString(word);
                Dictionary.Instance.AddWord(word, hashCode);
				//Status.Update(i + " out of " + stepsCount);
            }
			Status.Update("Done." + Environment.NewLine);
        }

        /// <summary>
        /// Check a text for spelling mistakes.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fixedText"> </param>
        /// <returns></returns>
        public List<Issue> Check(string text)
        {
            // Create list of words to be used as suggestions
            var issues = new List<Issue>();

            // Get words from text
            var tokenizer = new Tokenizer();
            List<Tokenizer.Token> tokens = tokenizer.Tokenize(text);

            // Check for spelling errors
            for (int i = 0; i < tokens.Count; i++)
            {
                // Get current word typed by the user
                string currentWord = tokens[i].Text;

                // Check if current word exists in dictionary
                if (!Dictionary.Instance.ContainsWord(currentWord))
                {
                    var newIssue = new Issue();
                    newIssue.Type = IssueType.Error;
                    newIssue.Position = tokens[i].Position;
                    newIssue.CurrentWord = currentWord;

                    // Get list of similar words from dictionary
                    List<string> similarWords = Dictionary.Instance.GetSimilarWords(currentWord);

                    // As there are not context in right side, only the predicted word is enough
                    // or then the TOP-1 of the similar words
                    if (similarWords.Count > 0)
                    {
                        newIssue.SuggestedWords.Add(similarWords[0]);
                    }

                    issues.Add(newIssue);
                }
            }

            return issues;
        }
    }
}
