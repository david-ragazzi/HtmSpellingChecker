namespace Evaluation
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        	this.buttonEvaluate = new System.Windows.Forms.Button();
        	this.textBoxTrainingData = new System.Windows.Forms.TextBox();
        	this.label1 = new System.Windows.Forms.Label();
        	this.textBoxResult = new System.Windows.Forms.TextBox();
        	this.label3 = new System.Windows.Forms.Label();
        	this.label4 = new System.Windows.Forms.Label();
        	this.textBoxMaxCycles = new System.Windows.Forms.TextBox();
        	this.label5 = new System.Windows.Forms.Label();
        	this.textBoxMaxSentences = new System.Windows.Forms.TextBox();
        	this.label2 = new System.Windows.Forms.Label();
        	this.textBoxMinSentences = new System.Windows.Forms.TextBox();
        	this.label6 = new System.Windows.Forms.Label();
        	this.textBoxIncrementSentences = new System.Windows.Forms.TextBox();
        	this.label7 = new System.Windows.Forms.Label();
        	this.SuspendLayout();
        	// 
        	// buttonEvaluate
        	// 
        	this.buttonEvaluate.Location = new System.Drawing.Point(962, 56);
        	this.buttonEvaluate.Name = "buttonEvaluate";
        	this.buttonEvaluate.Size = new System.Drawing.Size(75, 23);
        	this.buttonEvaluate.TabIndex = 5;
        	this.buttonEvaluate.Text = "Evaluate!";
        	this.buttonEvaluate.UseVisualStyleBackColor = true;
        	this.buttonEvaluate.Click += new System.EventHandler(this.buttonEvaluate_Click);
        	// 
        	// textBoxTrainingData
        	// 
        	this.textBoxTrainingData.Location = new System.Drawing.Point(111, 32);
        	this.textBoxTrainingData.Name = "textBoxTrainingData";
        	this.textBoxTrainingData.ReadOnly = true;
        	this.textBoxTrainingData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        	this.textBoxTrainingData.Size = new System.Drawing.Size(926, 20);
        	this.textBoxTrainingData.TabIndex = 1;
        	// 
        	// label1
        	// 
        	this.label1.AutoSize = true;
        	this.label1.Location = new System.Drawing.Point(13, 35);
        	this.label1.Name = "label1";
        	this.label1.Size = new System.Drawing.Size(95, 13);
        	this.label1.TabIndex = 2;
        	this.label1.Text = "Data to be trained:";
        	// 
        	// textBoxResult
        	// 
        	this.textBoxResult.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        	this.textBoxResult.Location = new System.Drawing.Point(12, 105);
        	this.textBoxResult.Multiline = true;
        	this.textBoxResult.Name = "textBoxResult";
        	this.textBoxResult.ReadOnly = true;
        	this.textBoxResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        	this.textBoxResult.Size = new System.Drawing.Size(1024, 500);
        	this.textBoxResult.TabIndex = 6;
        	// 
        	// label3
        	// 
        	this.label3.AutoSize = true;
        	this.label3.Location = new System.Drawing.Point(10, 89);
        	this.label3.Name = "label3";
        	this.label3.Size = new System.Drawing.Size(45, 13);
        	this.label3.TabIndex = 7;
        	this.label3.Text = "Results:";
        	// 
        	// label4
        	// 
        	this.label4.AutoSize = true;
        	this.label4.Location = new System.Drawing.Point(763, 61);
        	this.label4.Name = "label4";
        	this.label4.Size = new System.Drawing.Size(132, 13);
        	this.label4.TabIndex = 8;
        	this.label4.Text = "Max HTM Training Cycles:";
        	// 
        	// textBoxMaxCycles
        	// 
        	this.textBoxMaxCycles.Location = new System.Drawing.Point(898, 58);
        	this.textBoxMaxCycles.Name = "textBoxMaxCycles";
        	this.textBoxMaxCycles.Size = new System.Drawing.Size(46, 20);
        	this.textBoxMaxCycles.TabIndex = 4;
        	this.textBoxMaxCycles.Text = "5";
        	this.textBoxMaxCycles.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
        	// 
        	// label5
        	// 
        	this.label5.AutoSize = true;
        	this.label5.Location = new System.Drawing.Point(10, 434);
        	this.label5.Name = "label5";
        	this.label5.Size = new System.Drawing.Size(0, 13);
        	this.label5.TabIndex = 10;
        	// 
        	// textBoxMaxSentences
        	// 
        	this.textBoxMaxSentences.Location = new System.Drawing.Point(313, 58);
        	this.textBoxMaxSentences.Name = "textBoxMaxSentences";
        	this.textBoxMaxSentences.Size = new System.Drawing.Size(46, 20);
        	this.textBoxMaxSentences.TabIndex = 3;
        	this.textBoxMaxSentences.Text = "100";
        	this.textBoxMaxSentences.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
        	// 
        	// label2
        	// 
        	this.label2.AutoSize = true;
        	this.label2.Location = new System.Drawing.Point(280, 61);
        	this.label2.Name = "label2";
        	this.label2.Size = new System.Drawing.Size(30, 13);
        	this.label2.TabIndex = 14;
        	this.label2.Text = "Max:";
        	// 
        	// textBoxMinSentences
        	// 
        	this.textBoxMinSentences.Location = new System.Drawing.Point(111, 58);
        	this.textBoxMinSentences.Name = "textBoxMinSentences";
        	this.textBoxMinSentences.Size = new System.Drawing.Size(46, 20);
        	this.textBoxMinSentences.TabIndex = 15;
        	this.textBoxMinSentences.Text = "50";
        	this.textBoxMinSentences.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
        	// 
        	// label6
        	// 
        	this.label6.AutoSize = true;
        	this.label6.Location = new System.Drawing.Point(24, 62);
        	this.label6.Name = "label6";
        	this.label6.Size = new System.Drawing.Size(84, 13);
        	this.label6.TabIndex = 16;
        	this.label6.Text = "Sentences: Min:";
        	// 
        	// textBoxIncrementSentences
        	// 
        	this.textBoxIncrementSentences.Location = new System.Drawing.Point(228, 58);
        	this.textBoxIncrementSentences.Name = "textBoxIncrementSentences";
        	this.textBoxIncrementSentences.Size = new System.Drawing.Size(46, 20);
        	this.textBoxIncrementSentences.TabIndex = 17;
        	this.textBoxIncrementSentences.Text = "25";
        	this.textBoxIncrementSentences.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
        	// 
        	// label7
        	// 
        	this.label7.AutoSize = true;
        	this.label7.Location = new System.Drawing.Point(167, 62);
        	this.label7.Name = "label7";
        	this.label7.Size = new System.Drawing.Size(57, 13);
        	this.label7.TabIndex = 18;
        	this.label7.Text = "Increment:";
        	// 
        	// MainForm
        	// 
        	this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        	this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        	this.ClientSize = new System.Drawing.Size(1049, 617);
        	this.Controls.Add(this.textBoxIncrementSentences);
        	this.Controls.Add(this.label7);
        	this.Controls.Add(this.textBoxMinSentences);
        	this.Controls.Add(this.label6);
        	this.Controls.Add(this.textBoxMaxSentences);
        	this.Controls.Add(this.label2);
        	this.Controls.Add(this.label5);
        	this.Controls.Add(this.textBoxMaxCycles);
        	this.Controls.Add(this.label4);
        	this.Controls.Add(this.label3);
        	this.Controls.Add(this.textBoxResult);
        	this.Controls.Add(this.label1);
        	this.Controls.Add(this.textBoxTrainingData);
        	this.Controls.Add(this.buttonEvaluate);
        	this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        	this.Name = "MainForm";
        	this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        	this.Text = "Spelling Checkers Evaluation";
        	this.ResumeLayout(false);
        	this.PerformLayout();
        }
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBoxIncrementSentences;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxMinSentences;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;

        #endregion

        private System.Windows.Forms.Button buttonEvaluate;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxTrainingData;
        private System.Windows.Forms.TextBox textBoxResult;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxMaxCycles;
        private System.Windows.Forms.TextBox textBoxMaxSentences;
        private System.Windows.Forms.Label label2;
    }
}

