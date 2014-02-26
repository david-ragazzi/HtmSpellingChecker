using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Evaluation
{
	/// <summary>
	/// Description of Evaluation.
	/// </summary>
	public class Evaluation
    {
		enum SpellingMethod
		{
			EditMinDistance,
			NGram,
            Htm
		}
		
		struct MethodResult
		{
			public float AccuracyRealWords;
			public float AccuracyNonRealWords;
			public float Speed;
        }

		/// <summary>
        /// Enum to specify if an found issue is an error (word not found)
        /// or a warning (word found but without sense in context)
        /// </summary>
        public enum IssueType
        {
            Error,
            Warning
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
		
		private string _corpora = "";
		
		public Evaluation(string corpora)
		{
			this._corpora = corpora;
		}
		
		public void Process()
		{
			// Updates dictionary
			UpdateDictionary(this._corpora);
			
			// Prepares list with correct sentences
			List<string> correctSentences = SplitToSentences(this._corpora);
			
			// Prepares list with mispelled sentences
			var mispelledSentences = CreateMispelledSentences(correctSentences);
			
			// Evaluates all methods
            MethodResult editMinDistanceResult = CheckAccuracy(SpellingMethod.EditMinDistance, correctSentences, mispelledSentences);
			MethodResult ngramResult = CheckAccuracy(SpellingMethod.NGram, correctSentences, mispelledSentences);
			MethodResult htmResult = CheckAccuracy(SpellingMethod.Htm, correctSentences, mispelledSentences);
			
			// Show results
			string output = "EditMinDistance\tN-Gram\tHTM\t\n";
			output += "Accuracy (Real Words):\t" + editMinDistanceResult.AccuracyRealWords.ToString("#.###") + "\t" + ngramResult.AccuracyRealWords.ToString("#.###") + "\t" + htmResult.AccuracyRealWords.ToString("#.###") + "\n";
			output += "Accuracy (Non-Real Words):\t" + editMinDistanceResult.AccuracyNonRealWords.ToString("#.###") + "\t" + ngramResult.AccuracyNonRealWords.ToString("#.###") + "\t" + htmResult.AccuracyNonRealWords.ToString("#.###") + "\n";
			output += "Performance:\t" + editMinDistanceResult.Speed.ToString("hh:mm:ss") + "\t" + ngramResult.Speed.ToString("hh:mm:ss") + "\t" + htmResult.Speed.ToString("hh:mm:ss") + "\n";
			Console.WriteLine(output);
		}
		
		private void UpdateDictionary(string corpora)
		{
			// Get words from text
            var tokenizer = new Tokenizer();
            List<Tokenizer.Token> tokens = tokenizer.Tokenize(corpora);
            
            // Add each word to dictinory
            for (int i = 0; i < tokens.Count; i++)
            {
            	string word = tokens[i].Text;
            
	            // Get an hash representation to the word
	            uint hashCode = Dictionary.Instance.GetHashFromString(word);
	           	Dictionary.Instance.AddWord(word, hashCode);
            }
		}
		
		/// <summary>
		/// Splits a corpora to sentences.
		/// </summary>
		/// <param name="corpora"></param>
		/// <returns></returns>
		private List<string> SplitToSentences(string text)
		{
			var sentences = new List<string>();
			
			string tempSentence = "";
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				tempSentence += c;
				switch (c)
				{
					case '.':
					case '!':
					case '?':
					case ':':
						sentences.Add(tempSentence);
						tempSentence = "";
						break;
				}
			}
			
			return sentences;
		}
		
		/// <summary>
		/// Creates mispelled sentences from correct sentences.
		/// </summary>
		/// <param name="sentences"></param>
		/// <returns></returns>
		private List<Tuple<string, Issue>> CreateMispelledSentences(List<string> sentences)
		{
			var mispelledSentences = new List<Tuple<string, Issue>>();
			var random = new Random();
			
            var tokenizer = new Tokenizer();			
			for (int i = 0; i < sentences.Count; i++)
			{
				string sentence = sentences[i];
				
				// Gets tokens from text
				List<Tokenizer.Token> tokens = tokenizer.Tokenize(sentence);
				
				// Chooses a random word to be changed
				Tokenizer.Token randomToken;
				do
				{
					randomToken = tokens[random.Next() % tokens.Count];
				} while (!(randomToken.Type == Tokenizer.TokenType.Word && randomToken.Text.Length > 1))
				string correctWord = randomToken.text;
					
				// Define if mispelled word is a real word (0) or non-real word (1)
				IssueType issueType = (IssueType)(i % 2);
				
				// Define a mispelled word
				bool mispelledWordChosen = false;
				do
				{
					// Defines one of out 4 edits (insertion=0, deletion=1, substitution=2 or transposition=3)
					List<char> mispelledWord = correctWord.ToList();
					int editType = i % 4;
					switch (editType)
					{
						case 0:
							// Insertion
							
							// Chooses a random position to insert a char
							int pos = random.Next() % correctWord.Length;
							
							// Chooses a random char to be inserted
							char ch = (char)('a' + (random.Next() % 25));
							
							// Inserts 'ch' char at 'pos' position
							mispelledWord.Insert(pos, ch);
							
							break;
						case 2:
							// Deletion
							
							// Chooses a random position to delete a char
							pos = random.Next() % correctWord.Length;
							
							// Removes the char at 'pos' position
							mispelledWord.RemoveAt(pos);
							
							break;
						case 3:
							// Substitution						
													
							// Chooses a random position to replace a char
							pos = random.Next() % correctWord.Length;						
							
							// Chooses a random char to replace the other one
							do
							{
								ch = (char)('a' + (random.Next() % 25));
							} while (ch == correctWord[pos]);
							
							// Replaces the char at 'pos' position to 'ch' char
							mispelledWord[pos] = ch;
							
							break;
						case 4:
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
							char temp = mispelledWord[pos1];
							mispelledWord[pos1] = mispelledWord[pos2];
							mispelledWord[pos2] = temp;
							
							break;				
					}
					
					// If we need a real word we need check if 'mispelledWord' exists in dictionary
					if (issueType == IssueType.Warning)
					{
						if (Dictionary.Instance.ContainsWord(mispelledWord.ToString()))
						{
							mispelledWordChosen = true;
						}
					}
					else
					{
						mispelledWordChosen = true;
					}
				} while (mispelledWordChosen == false);
				
				// Create an issue
				var issue = new Issue();
				issue.Type = issueType;
				issue.CurrentWord = mispelledWord.ToString();
				issue.Position = randomToken.Position;
				issue.SuggestedWords.Add(correctWord);
				
				// Create the mispelled sentence
				string mispelledSentence = sentence.Substring(0, randomToken.Position - 1);
				mispelledSentence += mispelledWord.ToString();
				mispelledSentence += sentence.Substring(randomToken.Position + randomToken.Text.Length, sentence.Length - (randomToken.Position + randomToken.Text.Length));
				
				// Add mispelled sentence to list
				mispelledSentences.Add(new Tuple<mispelledSentence, issue>);
			}
			
			return mispelledSentences;
		}
		
		/// <summary>
		/// Check accuracy of a method.
		/// </summary>
		/// <param name="correctSentences"></param>
		/// <param name="fixedSentences"></param>
		private MethodResult CheckAccuracy(SpellingMethod spellingMethod, List<string> correctSentences, List<Tuple<string, Issue>> mispelledSentences)
		{
            var editMinDistanceSP = new SpellingChecker.EditMinDistance.Engine();
            var nGramSP = new SpellingChecker.NGram.Engine();
            var htmSP = new SpellingChecker.HTM.Engine();

			// Train spelling checkers engines with correct sentences
            if (spellingMethod == SpellingMethod.EditMinDistance)
            {
                editMinDistanceSP.Train(this._corpora);
            }
            else if (spellingMethod == SpellingMethod.NGram)
			{
				nGramSP.Train(this._corpora);
			}
			else if (spellingMethod == SpellingMethod.Htm)
			{
                htmSP.Train(this._corpora);
			}
			
			// Initialize statistics
			int sentencesCount = correctSentences.Count;
			int errorFailCount = 0;
			int warningFailCount = 0;
			
			// Compare sentence to sentence to check if they were really fixed
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < sentencesCount; i++)
			{
				string mispelledSentence = mispelledSentences[i].Item1;
				Issue emulatedIssue = mispelledSentences[i].Item2;				
				List<Issue> returnedIssues = new List<Evaluation.Issue>();
				
                if (spellingMethod == SpellingMethod.EditMinDistance)
				{
					List<SpellingChecker.EditMinDistance.Engine.Issue> editMinDistanceIssues = editMinDistanceSP.Check(mispelledSentence);
					returnedIssues = editMinDistanceIssues.Select(issue => new Issue
					{
						Type = (Evaluation.IssueType)issue.Type,
						Position = issue.Position,
						CurrentWord = issue.CurrentWord,
						SuggestedWords = issue.SuggestedWords
					}).ToList();
				}
				else if (spellingMethod == SpellingMethod.NGram)
				{
					List<SpellingChecker.NGram.Engine.Issue> nGramIssues = nGramSP.Check(mispelledSentence);
					returnedIssues = nGramIssues.Select(issue => new Issue
					{
						Type = (Evaluation.IssueType)issue.Type,
						Position = issue.Position,
						CurrentWord = issue.CurrentWord,
						SuggestedWords = issue.SuggestedWords
					}).ToList();
				}
                else if (spellingMethod == SpellingMethod.Htm)
				{
					List<SpellingChecker.HTM.Engine.Issue> htmIssues = htmSP.Check(mispelledSentence);
					returnedIssues = htmIssues.Select(issue => new Issue
					{
						Type = (Evaluation.IssueType)issue.Type,
						Position = issue.Position,
						CurrentWord = issue.CurrentWord,
						SuggestedWords = issue.SuggestedWords
					}).ToList();
				}
				
				// If sentences are not equal then we have a fail
				List<Issue> returnedErrorIssues = returnedIssues.Where(issue => issue.Type == IssueType.Error).ToList();
				List<Issue> returnedWarningIssues = returnedIssues.Where(issue => issue.Type == IssueType.Warning).ToList();
				if (!(emulatedIssue.Type == IssueType.Error && returnedErrorIssues.Count == 1 && returnedWarningIssues.Count == 0 && returnedErrorIssues[0].Position == emulatedIssue[0].Position && returnedErrorIssues[0].SuggestedWords[0] == emulatedIssue.SuggestedWords[0]))
				{
					errorFailCount += 1;
				}
				else if (!(emulatedIssue.Type == IssueType.Warning && returnedWarningIssues.Count == 1 && returnedErrorIssues.Count == 0 && returnedErrorIssues[0].Position == emulatedIssue[0].Position && returnedWarningIssues[0].SuggestedWords[0] == emulatedIssue.SuggestedWords[0]))
				{
					warningFailCount += 1;
				}
			}
			stopwatch.Stop();
            float timeElapsed = (float)stopwatch.Elapsed.TotalSeconds;
			
			// Calculate accuracy rate
			var result = new MethodResult();
            result.AccuracyNonRealWords = (float)sentencesCount / (sentencesCount + errorFailCount);
            result.AccuracyRealWords = (float)sentencesCount / (sentencesCount + warningFailCount);
			result.Speed = timeElapsed / sentencesCount; 
			
			return result;
		}
	}
}
