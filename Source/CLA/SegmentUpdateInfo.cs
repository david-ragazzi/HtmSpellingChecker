using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CLA
{
    public enum UpdateType
    {
        DueToActive,
        DueToPredictive
    }

    public enum ApplyUpdateTrigger
    {
        Active,
        Inactive,
        LongerPrediction
    }

	/// <summary>
    /// This data structure holds three pieces of information required to update a given 
	/// segment.
	/// </summary>
	/// This data structure holds three pieces of information required to update a given 
	/// segment: 
	///   a) segment reference (None if it's a new segment), 
	///   b) a list of existing active synapses, and 
	///   c) a flag indicating whether this segment should be marked as a sequence
	///      segment (defaults to false).
    /// The structure also determines which learning cells (at this time step) are available 
    /// to connect (add synapses to) should the segment get updated. If there is a prediction 
    /// radius set on the Region, the pool of learning cells is restricted to those within
    /// the radius.
    public class SegmentUpdateInfo
    {
		public Cell Cell;

		public Segment Segment;

		public bool AddNewSynapses;

		public int NumPredictionSteps;

		public int CreationTimeStep;

		public UpdateType UpdateType;

        public List<Synapse> ActiveDistalSynapses = new List<Synapse>();
        public List<Cell> CellsThatWillLearn = new List<Cell>();

		/// <summary>
		/// Generic randomizer
		/// </summary>
        private static Random _random = new Random();

        public SegmentUpdateInfo()
        {
        }

		/// <summary>
        /// Randomly sample m values from the Cell array of length n (m less than n).
        /// Runs in O(2m) worst case time.  Result is written to the result
		/// array of length m containing the randomly chosen cells.
		/// </summary>
		/// <param name="cells">Input Cells to randomly choose from.</param>
		/// <param name="result">The resulting random subset of Cells.</param>
		/// <param name="m">The number of random samples to take (m less than equal to result.Length)</param>
        public void RandomSample(ref List<Cell> cells, ref List<Cell> result, int numberRandomSamples)
        {
            int n = cells.Count;
            for (int i = n - numberRandomSamples; i < n; ++i)
            {
                int pos = _random.Next() % (i + 1);
                Cell cell = cells[pos];

                //if(subset ss contains item already) then use item[i] instead
                bool contains = result.Contains(cell);
                result.Add(contains
                               ? cells[i]
                               : cell);
            }
        }

		/// <summary>
		/// Create a new SegmentUpdateInfo that is to modify the state of the Region
		/// either by adding a new segment to a cell, new synapses to a segment,
		/// or updating permanences of existing synapses on some segment.
		/// </summary>
		/// <param name="cell">Cell the cell that is to have a segment added or updated.</param>
		/// <param name="segment">The segment that is to be updated (null here means a new
		/// segment is to be created on the parent cell).</param>
		/// <param name="activeDistalSynapses">The set of active synapses on the segment 
		/// that are to have their permanences updated.</param>
		/// <param name="addNewSynapses">Set to true if new synapses are to be added to the
        /// segment (or if new segment is being created) or false if no new synapses
		/// should be added instead only existing permanences updated.</param>
        public void Initialize(Cell cell, Segment segment, List<DistalSynapse> activeDistalSynapses, bool addNewSynapses, int creationTimeStep, UpdateType updateType)
        {
            // BMK Essential for temporal learning. Details specified 
            this.Cell = cell;
            this.Segment = segment;
            this.AddNewSynapses = addNewSynapses;
            this.CreationTimeStep = creationTimeStep;
            this.UpdateType = updateType;
            this.NumPredictionSteps = 1;

            if (activeDistalSynapses != null)
            {
                this.ActiveDistalSynapses = new List<Synapse>(activeDistalSynapses);
            }

            // Once synapses added, store here to visualize later
            // this.AddedSynapses = new List<DistalSynapse>();

            var learningCells = new List<Cell>(); //capture learning cells at this time step

            // If adding new synapses, find the current set of learning cells within
            // the Region and select a random subset of them to connect the segment to.
            // Do not add >1 synapse to the same cell on a given segment
            Region region = this.Cell.Column.Region;
            if (this.AddNewSynapses)
            {
                var segCells = new List<Cell>();

                if (this.Segment != null)
                {
                    // Fill the segCells list with each of the segment's existing synapses' input cells. 
                    foreach (Synapse syn in this.Segment.Synapses)
                    {
                        segCells.Add(((DistalSynapse)syn).InputSource);
                    }
                }

                // Only allow connecting to Columns within prediction radius
                Column cellColumn = this.Cell.Column;

                int minY, maxY, minX, maxX;

                // Determine bounds of Columns that we may make synapses with, those within the PredictionRadius.
                // If prediction radius is -1, it means 'no restriction'
                if (region.PredictionRadius > -1)
                {
                    Area predictionArea = cellColumn.DetermineColumnsWithinHypercolumnRadius(region.PredictionRadius);
                    minX = predictionArea.MinX;
                    minY = predictionArea.MinY;
                    maxX = predictionArea.MaxX;
                    maxY = predictionArea.MaxY;
                }
                else
                {
                    minY = 0;
                    maxY = region.SizeY - 1;
                    minX = 0;
                    maxX = region.SizeX - 1;
                }

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        // Get the current column at position x,y.
                        Column column = region.GetColumn(x, y);

                        // NOTE: There is no indication in the Numenta pseudocode that a cell shouldn't be able to have a 
                        // distal synapse from another cell in the same column. Therefore the below check is commented out.
                        //// Skip cells in our own column (don't connect to ourself)
                        //if (column == cell.GetColumn()) {
                        //	continue;
                        //}

                        // Add each of the column's cells that WasLearning, and that is not in the segCells list, to the learningCells list.
                        for (int cellIndex = 0; cellIndex < region.CellsPerCol; cellIndex++)
                        {
                            Cell curCell = column.Cells[cellIndex];

                            if (curCell.WasLearning && (!segCells.Contains(curCell)))
                            {
                                learningCells.Add(curCell);
                            }
                        }
                    }
                }

                segCells.Clear();
            }

            // Basic allowed number of new Synapses
            // TODO: Move this up so that if newSynCount is 0, the above compiling of a list of learning cells won't be done.
            int newSynCount = region.NewSynapsesCount;
            if (this.Segment != null)
            {
                newSynCount = Math.Max(0, newSynCount - ((activeDistalSynapses == null)
                                                             ? 0
                                                             : activeDistalSynapses.Count));
            }

            int numberLearningCells = learningCells.Count;

            // Clamp at -- of learn cells
            newSynCount = Math.Min(numberLearningCells, newSynCount);

            // Randomly choose synCount learning cells to add connections to
            if ((numberLearningCells > 0) && (newSynCount > 0))
            {
                var result = new List<Cell>();
                this.RandomSample(ref learningCells, ref result, newSynCount);
                this.CellsThatWillLearn = new List<Cell>(result);
                result.Clear();
            }

            learningCells.Clear();
        }

		/// <summary>
        /// Create a new segment on the update cell using connections from
		/// the set of learning cells for the update info.
		/// </summary>
        public Segment CreateCellSegment(int time)
        {
            Segment newSegment = this.Cell.CreateSegment(ref this.CellsThatWillLearn, time);
            //segment.getSynapses(_addedSynapses);//if UI wants to display added synapses
            newSegment.NumPredictionSteps = this.NumPredictionSteps;
            return newSegment;
        }

		/// <summary>
        /// Create new synapse connections to the segment to be updated using
		/// the set of learning cells in this update info.
		/// </summary>
        public void CreateSynapsesToLearningCells(SynapseParams synapseParams)
        {
            this.Segment.CreateSynapsesToLearningCells(ref this.CellsThatWillLearn, synapseParams);
        }
    }
}
