using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SpellingChecker.Common;

namespace Evaluation
{
	/// <summary>
    /// Description of AutomatedEvaluation.
	/// </summary>
	public class AutomatedEvaluation
    {
		public class TestSentence
		{
			public string MispelledSentence = "";
			public Issue Issue;
			public string LevenshteinSugestion = "";
			public string NGramSugestion = "";
			public string HtmSugestion = "";
		}

		public class PerformResult
		{
			public int SequencesCount;
			public int WordsCount;
			public IssueResult Error;
			public IssueResult Warning;
		}

		public class IssueResult
		{
			public MethodResult Levenshtein;
			public MethodResult NGram;
			public MethodResult Htm;
		}
		
		public class MethodResult
		{
			public float Accuracy;
			public float Performance;
        }

		public PerformResult Perform(int maxCycles, List<string> correctSentences, List<TestSentence> errorSentences, List<TestSentence> warningSentences)
		{
			// Get corpus from sentences
			string corpus = "";
			for (int i = 0; i < correctSentences.Count; i++) 
			{
				corpus += correctSentences[i];
			}

			// Get words from training text
			var tokenizer = new Tokenizer();
			var tokens = tokenizer.Tokenize(corpus).Where(w => w.Type == Tokenizer.TokenType.Word);
			var words = tokens.Select(t => t.Text).ToList();

			// Update status
			string output = "Checking corpus size..." + Environment.NewLine;
			output += "Sentences: " + correctSentences.Count + Environment.NewLine;
			output += "Words: " + words.Count + Environment.NewLine;
			Status.Update(output);

			// Create new instances of all engines
			Global.LevenshteinSP = new SpellingChecker.Levenshtein.Engine();
			Global.NGramSP = new SpellingChecker.NGram.Engine();
			Global.HtmSP = new SpellingChecker.HTM.Engine();

			// Train all engines
			Global.LevenshteinSP.Train(corpus);
			Global.NGramSP.Train(corpus);
			Global.HtmSP.Train(corpus, maxCycles);

			// Check accuracy for each type of error
			var performResult = new PerformResult();
			performResult.SequencesCount = correctSentences.Count;
			performResult.WordsCount = words.Count;
			performResult.Error = this.Check(IssueType.Error, correctSentences, errorSentences);
			performResult.Warning = this.Check(IssueType.Warning, correctSentences, warningSentences);

			return performResult;
		}

		private IssueResult Check(IssueType issueType, List<string> correctSentences, List<TestSentence> testSentences)
		{
			// Evaluates all methods
			var issueResult = new IssueResult();
			issueResult.Levenshtein = CheckAccuracy(Global.SpellingMethod.Levenshtein, issueType, correctSentences, testSentences);
			issueResult.NGram = CheckAccuracy(Global.SpellingMethod.NGram, issueType, correctSentences, testSentences);
			issueResult.Htm = CheckAccuracy(Global.SpellingMethod.Htm, issueType, correctSentences, testSentences);
			
			// Show tested sentences and results presented by each method
			int colLabelsOffset = 40;
			int colValuesOffset = 20;
			Status.Update("Correct".PadRight(colValuesOffset) + "Mispelled".PadRight(colValuesOffset) + "Levenshtein".PadRight(colValuesOffset) + "N-Gram".PadRight(colValuesOffset) + "HTM".PadRight(colValuesOffset));
			foreach (TestSentence testSentence in testSentences)
			{				
				Status.Update(testSentence.Issue.SuggestedWords[0].PadRight(colValuesOffset) + testSentence.Issue.CurrentWord.PadRight(colValuesOffset) + testSentence.LevenshteinSugestion.PadRight(colValuesOffset) + testSentence.NGramSugestion.PadRight(colValuesOffset) + testSentence.HtmSugestion.PadRight(colValuesOffset));
			}
			
			// Show type of test
			string type = "";
			switch (issueType)
			{
				case IssueType.Error:
					type = "Non-Words";
					break;
				case IssueType.Warning:
					type = "Real-Words";
					break;
			}
			
			// Show results
			colLabelsOffset = 30;
			colValuesOffset = 12;
			string output = Environment.NewLine;
			output += type.PadRight(colLabelsOffset) + "Levenshtein".PadLeft(colValuesOffset) + "N-Gram".PadLeft(colValuesOffset) + "HTM".PadLeft(colValuesOffset) + System.Environment.NewLine;
			output += "Accuracy (%):".PadRight(colLabelsOffset) + issueResult.Levenshtein.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + issueResult.NGram.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + issueResult.Htm.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + System.Environment.NewLine;
			output += "Performance (s):".PadRight(colLabelsOffset) + issueResult.Levenshtein.Performance.ToString("0.000").PadLeft(colValuesOffset) + issueResult.NGram.Performance.ToString("0.000").PadLeft(colValuesOffset) + issueResult.Htm.Performance.ToString("0.000").PadLeft(colValuesOffset) + System.Environment.NewLine;
			Status.Update(output);

			return issueResult;
		}
		
		public static void UpdateDictionary(List<string> words)
		{
            // Add each word to dictinory
			foreach (string word in words)
            {
				// Get an hash representation to the word
	            uint hashCode = Dictionary.Instance.GetHashFromString(word);
	           	Dictionary.Instance.AddWord(word, hashCode);
            }
		}
		
		/// <summary>
		/// Creates mispelled sentences from correct sentences.
		/// </summary>
		/// <param name="sentences"></param>
		/// <returns></returns>
		public static List<TestSentence> CreateTestSentences(IssueType issueType, List<string> sentences)
		{
			var testSentences = new List<TestSentence>();
			var random = new Random();

			string mispelledWord = "";
			EditType editType = EditType.Transposition;
			for (int i = 0; i < sentences.Count; i++)
			{
				string sentence = sentences[i];
				
				// Gets tokens from text
				var tokenizer = new Tokenizer();
				List<Tokenizer.Token> tokens = tokenizer.Tokenize(sentence);
				if (tokens.Count > 0)
				{
					bool mispelledWordChosen = false;
					do
					{
						// Chooses a random word to be changed
						Tokenizer.Token randomToken;
					    do
					    {
					        randomToken = tokens[random.Next() % tokens.Count];
					    } while (!(randomToken.Type == Tokenizer.TokenType.Word && randomToken.Text.Length > 1));
						string correctWord = randomToken.Text;

						if (issueType == IssueType.Error)
						{
							// Defines one of out 4 edits (insertion=0, deletion=1, substitution=2 or transposition=3)
							List<char> tempMispelledWord = correctWord.ToList();
							if (i % 2 == 0)
							{
								if ((int)editType == 3)
								{
									editType = (EditType)0;
								}
								else
								{
									editType += 1;
								}
							}
							switch (editType)
							{
								case EditType.Insertion:
									// Insertion
									
									// Chooses a random position to insert a char
									int pos = random.Next() % correctWord.Length;
									
									// Chooses a random char to be inserted
									char ch = (char)('a' + (random.Next() % 25));
									
									// Inserts 'ch' char at 'pos' position
									tempMispelledWord.Insert(pos, ch);
									
									break;
								case EditType.Deletion:
									// Deletion
									
									// Chooses a random position to delete a char
									pos = random.Next() % correctWord.Length;
									
									// Removes the char at 'pos' position
									tempMispelledWord.RemoveAt(pos);
									
									break;
								case EditType.Substitution:
									// Substitution			
															
									// Chooses a random position to replace a char
									pos = random.Next() % correctWord.Length;						
									
									// Chooses a random char to replace the other one
									do
									{
										ch = (char)('a' + (random.Next() % 25));
									} while (ch == correctWord[pos]);
									
									// Replaces the char at 'pos' position to 'ch' char
									tempMispelledWord[pos] = ch;
									
									break;
								case EditType.Transposition:
									// Transposition
									
									// Chooses a random position to swap a char
									int pos1 = random.Next() % correctWord.Length;
									
									// Chooses another random position at right
									// or at left if 'pos' already is the last char
									int pos2;
									if (pos1 == correctWord.Length - 1)
									{
										pos2 = pos1 - 1;
									}
									else
									{
										pos2 = pos1 + 1;
									}
									
									// Swap the char at 'pos1' position to another char at 'pos2' position
									char temp = tempMispelledWord[pos1];
									tempMispelledWord[pos1] = tempMispelledWord[pos2];
									tempMispelledWord[pos2] = temp;
									
									break;
							}

							// Convert mispelled word from list of chars to string
							mispelledWord = "";
							foreach (var c in tempMispelledWord.ToArray())
							{
								mispelledWord += c;
							}

							mispelledWordChosen = true;
						}
						else if (issueType == IssueType.Warning)
						{
							// If we need a real-word we need check if 'mispelledWord' exists in dictionary
							var similarWors = Dictionary.Instance.GetSimilarWords(correctWord);
							if (similarWors.Count > 0)
							{
								// Chooses a random position to get a word
								int pos = random.Next() % similarWors.Count;	
								mispelledWord = similarWors[pos];

								mispelledWordChosen = true;
							}
						}

						if (mispelledWordChosen)
						{
							// Create an issue
							var issue = new Issue();
							issue.Type = issueType;
							issue.CurrentWord = mispelledWord.ToString().ToLower();
							issue.Position = randomToken.Position;
							issue.SuggestedWords.Add(correctWord.ToLower());
							
							// Create the mispelled sentence
							string mispelledSentence = sentence.Substring(0, randomToken.Position);
							mispelledSentence += mispelledWord.ToString();
							mispelledSentence += sentence.Substring(randomToken.Position + randomToken.Text.Length, sentence.Length - (randomToken.Position + randomToken.Text.Length));
							
							// Add mispelled sentence to list
							var testSentence = new TestSentence();
							testSentence.MispelledSentence = mispelledSentence;
							testSentence.Issue = issue;
							testSentences.Add(testSentence);
						}

					} while (mispelledWordChosen == false);
				}
			}
			
			return testSentences;
		}
		
		/// <summary>
		/// Check accuracy of a method.
		/// </summary>
		/// <param name="correctSentences"></param>
		/// <param name="fixedSentences"></param>
		private MethodResult CheckAccuracy(Global.SpellingMethod spellingMethod, IssueType requiredIssueType, List<string> correctSentences, List<TestSentence> testSentences)
		{
            // Initialize statistics
			int sentencesCount = correctSentences.Count;
			int failCount = 0;
			
			// Compare sentence to sentence to check if they were really fixed
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < sentencesCount; i++)
			{
				string mispelledSentence = testSentences[i].MispelledSentence;
				Issue emulatedIssue = testSentences[i].Issue;
                List<Issue> returnedIssues = new List<Issue>();
                if (spellingMethod == Global.SpellingMethod.Levenshtein)
				{
                    returnedIssues = Global.LevenshteinSP.Check(mispelledSentence);
				}
				else if (spellingMethod == Global.SpellingMethod.NGram)
				{
                    returnedIssues = Global.NGramSP.Check(mispelledSentence, requiredIssueType);
				}
                else if (spellingMethod == Global.SpellingMethod.Htm)
				{
                    returnedIssues = Global.HtmSP.Check(mispelledSentence, requiredIssueType);
				}
				
				// Check if returned issues are the same that were emulated
				bool fail = true;
				if (returnedIssues.Count == 1 && returnedIssues[0].Position == emulatedIssue.Position && returnedIssues[0].SuggestedWords.Count > 0 )
				{
					string suggestedWord = returnedIssues[0].SuggestedWords[0].ToLower();
					if (spellingMethod == Global.SpellingMethod.Levenshtein)
					{
						testSentences[i].LevenshteinSugestion = suggestedWord;
					}
					else if (spellingMethod == Global.SpellingMethod.NGram)
					{
						testSentences[i].NGramSugestion = suggestedWord;
					}
					else if (spellingMethod == Global.SpellingMethod.Htm)
					{
						testSentences[i].HtmSugestion = suggestedWord;
					}
					
					// If suggestion matches correct word, we dont have a fail
					if (returnedIssues[0].Type == emulatedIssue.Type && suggestedWord == emulatedIssue.SuggestedWords[0])
					{
						fail = false;
					}
				}
				
				// Update statistics
				if (fail)
				{
					failCount += 1;
				}			
			}
			stopwatch.Stop();
            float timeElapsed = (float)stopwatch.Elapsed.TotalSeconds;
			
			// Calculate accuracy rate
			var result = new MethodResult();
			result.Accuracy = ((float)(sentencesCount - failCount) / sentencesCount) * 100;
            result.Performance = (timeElapsed / sentencesCount) * 100;
			
			return result;
		}
	}
}
