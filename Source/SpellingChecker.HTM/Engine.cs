using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CLA;
using SpellingChecker.Common;

namespace SpellingChecker.HTM
{
    public class Engine
    {
        // This component will be the main interface to HTM neural network
        private HtmController _htmControler = new HtmController();
        
        private WordEncoder _wordEncoder = new WordEncoder();
        
        /// <summary>
        /// Trains the spelling checker with a corpus.
        /// </summary>
        /// <param name="corpus"></param>
        /// <param name="maxCycles">How many times spelling checker should be trained with this text.</param>
        public void Train(string corpus, int maxCycles)
        {
            // Get words from training text
            var tokenizer = new Tokenizer();
            List<Tokenizer.Token> tokens = tokenizer.Tokenize(corpus);

			// Initialize HTM Processor
			Status.Update("Initializing HTM engine...");
			this._htmControler.Initialize();

			// Create the data source for the current word list
            var wordDataSource = new ListDataSource(new List<object>(tokens.Select(i => i.Text).ToList()));
            this._htmControler.Sensor.DataSource = wordDataSource;
            
            // Set the encoder for the sensor
            this._wordEncoder.UpdateDictionary = true;
            this._htmControler.Sensor.Encoder = this._wordEncoder;

            // Train
			Status.Update("Trainning HTM engine...");
			Global.AllowSpatialLearning = true;
            int cycle = 0;
            float accuracy = 0;
            while (cycle < maxCycles)
            {
				if (cycle > 1)
				{
					Global.AllowSpatialLearning = false;
				}

	            int incorrectPredictions = 0;
	            string currentWord = "";
	            string nextWord = "";
	            string predictedWord = "";
	            for (int i = 0; i < tokens.Count; i++)
	            {
	                this._htmControler.NextTimeStep();
	                
	                if (i < tokens.Count - 1 && tokens[i].Type == Tokenizer.TokenType.Word && tokens[i+1].Type == Tokenizer.TokenType.Word)
	                {
		                currentWord = tokens[i].Text;
	                	nextWord = tokens[i+1].Text;
		                if (i > 0 && predictedWord != currentWord)
		                {
		                    incorrectPredictions += 1;
		                }
		                
		                predictedWord = "";
		                List<HtmController.Prediction> predictions = this._htmControler.GetOutput();
		                predictions = predictions.Where(p => Word.IsSimilar((string)p.Value, nextWord)).ToList();
						//Status.Update("Current: " + currentWord);
		                if (predictions.Count > 0)
		                {
		                	predictedWord = (string)predictions[0].Value;
							/*foreach (var prediction in predictions)
		                	{
								Status.Update("Predicted: " + prediction.Value + ", Probability: " + prediction.Probability);
		                	}*/
		                }
	                }
	            }

				float newAccuracy = ((float)(tokens.Count - incorrectPredictions) / tokens.Count) * 100;
				if (newAccuracy == accuracy)
				{
					break;
				}
				accuracy = newAccuracy;
	            cycle += 1;

                Status.Update("Cycles: " + cycle + ", Accuracy (%): " + accuracy);
			}
			Global.AllowSpatialLearning = true;

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

            // Create the data source for the current word list
            var wordDataSource = new ListDataSource(new List<object>(tokens.Select(i => i.Text).ToList()));
            this._htmControler.Sensor.DataSource = wordDataSource;

            // Check for spelling errors
			Global.AllowSpatialLearning = false;
			Global.AllowTemporalLearning = false;
            this._wordEncoder.UpdateDictionary = false;
            for (int i = 0; i < tokens.Count; i++)
            {
                // Get current word typed by the user
                string currentWord = tokens[i].Text;
                
                // Get the possible but similar words for the current word
                List<HtmController.Prediction> predictions = this._htmControler.GetOutput();
                predictions = predictions.Where(p => Word.IsSimilar((string)p.Value, currentWord)).ToList();
				this._htmControler.NextTimeStep();

                // Check if current word exists in dictionary
				bool realWord = Dictionary.Instance.ContainsWord(currentWord);
				Issue newIssue = null;
				if (realWord)
				{
					if (requiredIssueType == IssueType.All || requiredIssueType == IssueType.Warning)
					{
						for (int j = 0; j < predictions.Count; j++)
						{
							string predictedWord = (string)predictions[j].Value;
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
	                	string predictedWord = (string)predictions[j].Value;
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
            			// Create the data source for testing words
            			var testDataSource = new ListDataSource(new List<object>());
            			testDataSource.List.Add("");
            			
                    	// Change datasource to test words
                        this._htmControler.Sensor.DataSource = testDataSource;
                        
                        // Try each word in order to verify if word
                        foreach (string testingWord in testingWords)
                        {
                        	testDataSource.List[0] = testingWord;

                            // Get the possible but similar word for the testing word
                            this._htmControler.NextTimeStep();
			                string predictedWord = "";
			                predictions = this._htmControler.GetOutput();
			                predictions = predictions.Where(p => Word.IsSimilar((string)p.Value, currentWord)).ToList();
			                for (int j = 0; j < predictions.Count; j++)
			                {
			                	predictedWord = (string)predictions[j].Value;
				                
			                	// If next word is very close to predicted word then use the testing word as suggestion
	                            if (predictedWord == nextWord)
	                            {
	                                newIssue.SuggestedWords.Add(testingWord);
	                                break;
	                            }
			                }
                        }
                        
                        // Change back current datasource to check text
            			this._htmControler.Sensor.DataSource = wordDataSource;
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
			this._wordEncoder.UpdateDictionary = true;
			Global.AllowSpatialLearning = true;
            Global.AllowTemporalLearning = true;
            
			return issues;
        }
    }
}
