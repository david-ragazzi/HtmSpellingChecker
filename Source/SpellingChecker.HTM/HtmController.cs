using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using CLA;
using Region = CLA.Region;

namespace SpellingChecker.HTM
{
    public class HtmController
    {
    	public class Prediction
    	{
    		public float[,,] Data;
    		public object Value;
    		public float Probability;    		
    	}
    	
        // HTM elements to handle word sequence
        public Region Region;
        public Sensor Sensor;

        /// <summary>
        /// Constructor.
        /// </summary>
        public HtmController()
        {
        }

        /// <summary>
        /// Initialize the network starting from top region.
        /// </summary>
        public void Initialize()
        {
			// Set project parameters
            Global.AllowSpatialLearning = true;
            Global.AllowTemporalLearning = true;
            Global.AllowBoosting = false;

            // Create the new input.
            this.Sensor = new Sensor("Input1", WordEncoder.MaxHashSize, 10, 1, null, null);
            this.Sensor.Index = 1;

            // Set Proximal Synapse parameter
            var proximalSynapseParams = new SynapseParams();
			proximalSynapseParams.InitialPermanence = 0.30f;
			proximalSynapseParams.ConnectedPerm = 0.24f;
			proximalSynapseParams.PermanenceInc = 0.0060f;
			proximalSynapseParams.PermanenceDec = 0.0045f;

            // Set Distal Synapse parameter
            var distalSynapseParams = new SynapseParams();
            distalSynapseParams.InitialPermanence = proximalSynapseParams.InitialPermanence;
            distalSynapseParams.ConnectedPerm = proximalSynapseParams.ConnectedPerm;
            distalSynapseParams.PermanenceInc = proximalSynapseParams.PermanenceInc;
            distalSynapseParams.PermanenceDec = proximalSynapseParams.PermanenceDec;

            // Set Region parameters
            string regionId = "Region1";
			int regionSizeX = 12;
			int regionSizeY = 12;
            bool hardcodedSpatial = false;
            float percentageInputPerCol = 100;
            float percentageMinOverlap = 0;
            float percentageLocalActivity = 1;
            float maxBoost = -1;
            float boostRate = 0.05f;
            int spatialLearningStartTime = -1;
            int spatialLearningEndTime = -1;
            int temporalLearningStartTime = -1;
            int temporalLearningEndTime = -1;
            int boostingStartTime = -1;
            int boostingEndTime = -1;
            int min_MinOverlapToReuseSegment = 2;
            int max_MinOverlapToReuseSegment = 2;
            int predictionRadius = -1;
			int cellsPerColumn = 120;
            int hypercolumnDiameter = 1;
            int segmentActivateThreshold = 1;
			int newNumberSynapses = 1;
            bool outputColumnActivity = false;
            bool outputCellActivity = true;
            var inhibitionType = InhibitionType.Automatic;
            int inhibitionRadius = -1;

            // Create the new Region.
            this.Region = new Region(regionId, new Point(regionSizeX, regionSizeY), hypercolumnDiameter, proximalSynapseParams, distalSynapseParams, percentageInputPerCol / 100.0f, percentageMinOverlap / 100.0f, predictionRadius, inhibitionType, inhibitionRadius, percentageLocalActivity / 100.0f, boostRate, maxBoost, spatialLearningStartTime, spatialLearningEndTime, temporalLearningStartTime, temporalLearningEndTime, boostingStartTime, boostingEndTime, cellsPerColumn, segmentActivateThreshold, newNumberSynapses, min_MinOverlapToReuseSegment, max_MinOverlapToReuseSegment, hardcodedSpatial, outputColumnActivity, outputCellActivity);
            this.Region.Index = 1;
            this.Region.AddInput(this.Sensor, 15);
            this.Region.Initialize();
        }

        /// <summary>
        /// Perfoms actions related to time step progression.
        /// </summary>
        public void NextTimeStep()
        {
            // Perfom actions related to time step progression of the top region
            this.Region.NextTimeStep();
        }

        /// <summary>
        /// Get the output to the current time step.
        /// The output is a string list representing words that were predicted by 'region'.
        /// </summary>
        public List<Prediction> GetOutput()
        {
        	var predictions = new List<Prediction>();
        	
            float maxPermanence = 0.0f;

            Column lastCol = null;
            Prediction prediction = null;
            for (int colY = 0; colY < this.Region.SizeY; colY++)
            {
                for (int colX = 0; colX < this.Region.SizeX; colX++)
                {
                    // Get a pointer to the current column.
                    Column curCol = this.Region.GetColumn(colX, colY);

                    // Set projectCol to true if any cell in this column is predicting for the next time step (a sequence prediction).
                    for (int cellIndex = 0; cellIndex < this.Region.CellsPerCol; cellIndex++)
                    {
                        Cell curCell = curCol.Cells[cellIndex];

                        if (curCell.IsPredicting && (curCell.NumPredictionSteps == 1))
                        {
                            // Iterate through all proximal synapses of this column that's being projected...
                            foreach (Synapse syn in curCol.ProximalSegment.Synapses)
                            {
                                var pSyn = (ProximalSynapse)syn;

                                // If the current proximal synapse connects from the DataSpace being displayed in this view...
                                if (pSyn.InputSource == this.Sensor)
                                {
				                    if (curCol != lastCol)
				                    {
				                    	prediction = new Prediction();
				        				prediction.Data = new float[this.Sensor.SizeX, this.Sensor.SizeY, 1];
				        				predictions.Add(prediction);
				        				lastCol = curCol;
				                    }
                                	
                                    // Add the current synapse's permanence to the imageVal corresponding to the cell in the DataSpace being
                                    // displayed by this view, that the Synapse connects from.
                                    prediction.Data[pSyn.InputPoint.X, pSyn.InputPoint.Y, 0] += pSyn.Permanence;
                                    prediction.Probability += pSyn.Permanence;

                                    // Record the maximum imageVal among all cells, to use later for normalization.
                                    maxPermanence = Math.Max(maxPermanence, prediction.Probability);
                                }
                            }
                            break;
                        }
                    }
                }
            }

        	for (int i = 0; i < predictions.Count; i++)
        	{
                // Normalize each probability proportional to maxPermanence.
	            if (maxPermanence > 0.0f)
	            {	                
		        	predictions[i].Probability /= maxPermanence;
	            }
	            predictions[i].Probability *= 100;
	            predictions[i].Value = (string)this.Sensor.Encoder.DecodeFromHtm(predictions[i].Data);
        	}
            predictions = predictions.Where(p => p.Value != null).OrderByDescending(p => p.Probability).ToList();
            
            //TODO: Remove:
            /*int index = 1;
            for (int i = 0; i < predictions.Count; i++)
            {
            	string str = Environment.NewLine + "Index: " + index + ", Word predicted: " + predictions[i].Value + ", Probability (%): " + predictions[i].Probability + Environment.NewLine;
		        for (int y = 0; y < this.Sensor.SizeY; y++)
		        {
		            for (int x = 0; x < this.Sensor.SizeX; x++)
		            {
		                str += predictions[i].Data[x, y, 0].ToString("0.00") + "|";
		            }
		            str += Environment.NewLine;
		        }
		        Status.Update(str);
		        index += 1;
            }*/

            return predictions;
        }
    }
}
