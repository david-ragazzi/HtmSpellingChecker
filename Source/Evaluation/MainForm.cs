using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SpellingChecker.Common;

namespace Evaluation
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.InitializeComponent();
			Status.StatusLabel = this.textBoxResult;
            this.textBoxTrainingData.Text = Application.StartupPath.Replace("\\", "/") + "/Data/corpus.txt";
        }

        private void buttonEvaluate_Click(object sender, EventArgs e)
		{
			int maxCycles = Convert.ToInt32(this.textBoxMaxCycles.Text);

			// Get corpus from file
            Status.Update("Reading file...");
			string corpusFile = textBoxTrainingData.Text;
			var fileStream = new FileStream(corpusFile, FileMode.Open);
            var streamReader = new StreamReader(fileStream);
            string corpus = streamReader.ReadToEnd();
			corpus = corpus.Replace("\r", "");

			// Get words from training text
			var tokenizer = new Tokenizer();
			var tokens = tokenizer.Tokenize(corpus).Where(w => w.Type == Tokenizer.TokenType.Word);
			var words = tokens.Select(t => t.Text).ToList();

			// Updates dictionary with words gathered from text
            Status.Update("Updating dictionary...");
			AutomatedEvaluation.UpdateDictionary(words);

			// Get sentences from training text
			var correctSentences = Utils.SplitToSentences(corpus);
			
			// Prepares list with mispelled sentences
            Status.Update("Creating test sentences...");
			var errorSentences = AutomatedEvaluation.CreateTestSentences(IssueType.Error, correctSentences);
			var warningSentences = AutomatedEvaluation.CreateTestSentences(IssueType.Warning, correctSentences);

			// Evaluate corpus with incremental sizes
			int minSentences = Convert.ToInt32(this.textBoxMinSentences.Text);
			int increment = Convert.ToInt32(this.textBoxIncrementSentences.Text);
			int maxSentences = Convert.ToInt32(this.textBoxMaxSentences.Text);
			int test = 1;
			var performResults = new List<AutomatedEvaluation.PerformResult>();
			for (int i = minSentences; i <= maxSentences; i += increment, test++)
			{
				Status.Update(System.Environment.NewLine + "===== Test #" + test.ToString("00") + " =====");

				var fullEvaluation = new AutomatedEvaluation();
				var performResult = fullEvaluation.Perform(maxCycles, correctSentences.GetRange(0, i), errorSentences.GetRange(0, i), warningSentences.GetRange(0, i));

				performResults.Add(performResult);
			}

			Status.Update(System.Environment.NewLine + "===== Summary =====");
			string output = "";
			int colLabelsOffset = 5;
			int colValuesOffset = 12;

			// Show summary of the corpus sizes used for tests
			output = System.Environment.NewLine + "Corpuses size:" + System.Environment.NewLine;
			output += "Test".PadLeft(colValuesOffset) + "Sequences".PadLeft(colValuesOffset) + "Words".PadLeft(colValuesOffset) + System.Environment.NewLine;
			for (int i = 0; i < performResults.Count; i++)
			{
				output += (i+1).ToString("00").PadLeft(colValuesOffset) + performResults[i].SequencesCount.ToString().PadLeft(colValuesOffset) + performResults[i].WordsCount.ToString().PadLeft(colValuesOffset) + System.Environment.NewLine;
			}
			Status.Update(output);

			// Show summary of the accuracy test for non-word errors
			output = System.Environment.NewLine + "Accuracy for Non-Word Errors:" + System.Environment.NewLine;
			output += "Test".PadLeft(colValuesOffset) + "Levenshtein".PadLeft(colValuesOffset) + "N-Gram".PadLeft(colValuesOffset) + "HTM".PadLeft(colValuesOffset) + System.Environment.NewLine;
			for (int i = 0; i < performResults.Count; i++)
			{
				 output += (i+1).ToString("00").PadLeft(colValuesOffset) + performResults[i].Error.Levenshtein.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Error.NGram.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Error.Htm.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + System.Environment.NewLine;
			}
			Status.Update(output);

			// Show summary of the accuracy test for non-word errors
			output = System.Environment.NewLine + "Performance for Non-Word Errors:" + System.Environment.NewLine;
			output += "Test".PadLeft(colValuesOffset) + "Levenshtein".PadLeft(colValuesOffset) + "N-Gram".PadLeft(colValuesOffset) + "HTM".PadLeft(colValuesOffset) + System.Environment.NewLine;
			for (int i = 0; i < performResults.Count; i++)
			{
				output += (i+1).ToString("00").PadLeft(colValuesOffset) + performResults[i].Error.Levenshtein.Performance.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Error.NGram.Performance.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Error.Htm.Performance.ToString("0.000").PadLeft(colValuesOffset) + System.Environment.NewLine;
			}
			Status.Update(output);

			// Show summary of the accuracy test for real-word errors
			output = System.Environment.NewLine + "Accuracy for Real-Word Errors:" + System.Environment.NewLine;
			output += "Test".PadLeft(colValuesOffset) + "Levenshtein".PadLeft(colValuesOffset) + "N-Gram".PadLeft(colValuesOffset) + "HTM".PadLeft(colValuesOffset) + System.Environment.NewLine;
			for (int i = 0; i < performResults.Count; i++)
			{
				output += (i+1).ToString("00").PadLeft(colValuesOffset) + performResults[i].Warning.Levenshtein.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Warning.NGram.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Warning.Htm.Accuracy.ToString("0.000").PadLeft(colValuesOffset) + System.Environment.NewLine;
			}
			Status.Update(output);

			// Show summary of the accuracy test for real-word errors
			output = System.Environment.NewLine + "Performance for Real-Word Errors:" + System.Environment.NewLine;
			output += "Test".PadLeft(colValuesOffset) + "Levenshtein".PadLeft(colValuesOffset) + "N-Gram".PadLeft(colValuesOffset) + "HTM".PadLeft(colValuesOffset) + System.Environment.NewLine;
			for (int i = 0; i < performResults.Count; i++)
			{
				output += (i+1).ToString("00").PadLeft(colValuesOffset) + performResults[i].Warning.Levenshtein.Performance.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Warning.NGram.Performance.ToString("0.000").PadLeft(colValuesOffset) + performResults[i].Warning.Htm.Performance.ToString("0.000").PadLeft(colValuesOffset) + System.Environment.NewLine;
			}
			Status.Update(output);

            // Update controls
            this.buttonEvaluate.Enabled = false;
        }
    }
}
