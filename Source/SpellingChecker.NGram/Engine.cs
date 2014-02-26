using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SpellingChecker.Common;

namespace SpellingChecker.NGram
{
    public class Engine
    {
        /// <summary>
        /// A class to represent a n-gram and its frequency in a corpus.
        /// </summary>
        /// <param name="corpus"></param>
        private class NGram
        {
        	public string Word1;
        	public string Word2;
        	public string Word3;
        	public int Frequency;
        }
        
        // List of N-Grams
        private List<NGram> _nGrams = new List<NGram>();

        /// <summary>
        /// Trains the spelling checker with a corpus.
        /// </summary>
        /// <param name="corpus"></param>
        /// <param name="repetitionCycles">How many times spelling checker should be trained with this text.</param>
        public void Train(string corpus)
        {
            // Gets words from training text
            var tokenizer = new Tokenizer();
            List<Tokenizer.Token> tokens = tokenizer.Tokenize(corpus);

            // Train
			Status.Update("Trainning NGram engine...");
            for (int i = 0; i < tokens.Count; i++)
            {
            	string word1 = tokens[i].Text;
            	
                if (i < (tokens.Count - 2) && tokens[i].Type == Tokenizer.TokenType.Word && tokens[i + 1].Type == Tokenizer.TokenType.Word && tokens[i + 2].Type == Tokenizer.TokenType.Word)
                {
                	string word2 = tokens[i+1].Text;
                	string word3 = tokens[i+2].Text;
                	
                	// Checks it this ngram already exists in list. Otherwise, add it to list.
                	NGram ngram = this._nGrams.Find(item => item.Word1 == word1 && item.Word2 == word2 && item.Word3 == word3);
                	if (ngram == null)
                	{
                		ngram = new Engine.NGram();
                		ngram.Word1 = word1;
                		ngram.Word2 = word2;
                		ngram.Word3 = word3;
                		ngram.Frequency = 0;
                		_nGrams.Add(ngram);
                	}
                	
                	// Increments frequency of this ngram
                	ngram.Frequency += 1;
                }

                // Get an hash representation to the word
                uint hashCode = Dictionary.Instance.GetHashFromString(word1);
                Dictionary.Instance.AddWord(word1, hashCode);
                
				//Status.Update(i + " out of " + stepsCount);
            }

            int incorrectPredictions = 0;
			string currentWord = "";
            string predictedWord = "";
            for (int i = 0; i < tokens.Count; i++)
            {
                string previousWord = "";
                if (i > 0)
                {
					previousWord = tokens[i-1].Text;
                }
				currentWord = tokens[i].Text;
				string nextWord = "";
				if (i < (tokens.Count - 1))
				{
					nextWord = tokens[i+1].Text;
				}

                if (i > 0 && predictedWord != currentWord)
                {
                    incorrectPredictions += 1;
                }                
                predictedWord = "";
                if (previousWord != "" && currentWord != "")
                {
					List<NGram> predictedNgrams = this._nGrams.FindAll(item => item.Word1 == previousWord && item.Word2 == currentWord && Word.IsSimilar(item.Word3, nextWord)).OrderByDescending(item => item.Frequency).ToList();
	                if (predictedNgrams.Count > 0)
	                {
	                	predictedWord = predictedNgrams[0].Word3;
	                }
                }
				//Status.Update(currentWord + " => " + predictedWord);
            }
            float accuracy = ((float)(tokens.Count - incorrectPredictions) / tokens.Count) * 100;
			Status.Update("Accuracy (%): " + accuracy);
			Status.Update("Done." + Environment.NewLine);
        }

        /// <summary>
        /// Check a text for spelling mistakes.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fixedText"> </param>
        /// <returns></returns>
        public List<Issue> Check(string text, IssueType requiredIssueType = IssueType.All)
        {
            // Create list of words to be used as suggestions
            var issues = new List<Issue>();

            // Get words from text
            var tokenizer = new Tokenizer();
            List<Tokenizer.Token> tokens = tokenizer.Tokenize(text);

            // Check for spelling errors
            for (int i = 0; i < tokens.Count; i++)
            {
                // Get previous word typed by the user
                string previousPreviousWord = "";
                string previousWord = "";
                if (i > 0)
                {
					previousWord = tokens[i-1].Text;
					if (i > 1)
                	{
						previousPreviousWord = tokens[i-2].Text;
                	}
                }
                
                // Get current word typed by the user
                string currentWord = tokens[i].Text;

                // Get the possible but similar words for the current word
                List<NGram> predictions = new List<NGram>();
                if (previousPreviousWord != "" && previousWord != "" && currentWord != "")
                {
                    predictions = this._nGrams.FindAll(item => item.Word1 == previousPreviousWord && item.Word2 == previousWord && Word.IsSimilar(item.Word3, currentWord)).OrderByDescending(item => item.Frequency).ToList();
                }

				// Check if current word exists in dictionary
				bool realWord = Dictionary.Instance.ContainsWord(currentWord);
				Issue newIssue = null;
				if (realWord)
				{
					if (requiredIssueType == IssueType.All || requiredIssueType == IssueType.Warning)
					{
						for (int j = 0; j < predictions.Count; j++)
						{
							string predictedWord = (string)predictions[j].Word3;
							if (currentWord != predictedWord && Word.Compare(currentWord, predictedWord) > 0.5f)
							{
								newIssue = new Issue();
								newIssue.Type = IssueType.Warning;
								break;
							}
						}
					}
				}
				else
				{
					if (requiredIssueType == IssueType.All || requiredIssueType == IssueType.Error)
					{
						newIssue = new Issue();
						newIssue.Type = IssueType.Error;
					}
				}

				// If there is an issue
				if (newIssue != null)
                {
					newIssue.Position = tokens[i].Position;
					newIssue.CurrentWord = currentWord;

                    // Create list of words with the predicted words and the similar words
                    // to be tested
                    List<String> testingWords = new List<String>();
                    for (int j = 0; j < predictions.Count; j++)
                    {
                        string predictedWord = predictions[j].Word3;
                        testingWords.Add(predictedWord);
                    }
					if (!realWord)
					{
						// Get list of similar words from dictionary
						List<string> similarWords = Dictionary.Instance.GetSimilarWords(currentWord);
						for (int j = 0; j < similarWords.Count; j++)
						{
							string similarWord = similarWords[j];

							// This avoid duplicate words gathered from predictions
							if (!testingWords.Contains(similarWord))
							{
								testingWords.Add(similarWord);
							}
						}
					}

                    // Get next word typed by the user
                    string nextWord = "";
                    if (i != (tokens.Count - 1))
                    {
                        nextWord = tokens[i + 1].Text;
                    }

                    // If exists context on right side of text...
                    if (nextWord != "")
                    {
                        // Try each word in order to verify if word
                        foreach (string testingWord in testingWords)
                        {
                            // Get the possible but similar word for the testing word
			                string predictedWord = "";
			                if (previousWord != "")
			                {
                                predictions = this._nGrams.FindAll(item => item.Word1 == previousWord && item.Word2 == testingWord && Word.IsSimilar(item.Word3, nextWord)).OrderByDescending(item => item.Frequency).ToList();
                                for (int j = 0; j < predictions.Count; j++)
                                {
                                    predictedWord = predictions[j].Word3;

                                    // If next word is very close to predicted word then use the testing word as suggestion
                                    if (predictedWord == nextWord)
                                    {
                                        newIssue.SuggestedWords.Add(testingWord);
                                        break;
                                    }
                                }
			                }
                        }
                    }

                    // If there is not context in right side, only the predicted word is enough
                    // or then the TOP-1 of the testing words
                    if (newIssue.SuggestedWords.Count == 0 && testingWords.Count > 0)
                    {
                        newIssue.SuggestedWords.Add(testingWords[0]);
                    }

                    issues.Add(newIssue);
                }
            }

            return issues;
        }
    }
}
