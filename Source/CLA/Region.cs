using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace CLA
{
	/// <summary>
	/// Determines whether inhibition is set given a radius, or is automatically determined based on average receptive field size.
	/// </summary>
    public enum InhibitionType
    {
        Automatic = 0,
        Radius = 1
    }

	/// <summary>
	/// Represents an input for the HTM region.
	/// <summary>
    public class Input
    {
        public DataSpace DataSpace;

		/// <summary>
        // The input radius corresponding to this input DataSpace.
        // Furthest number of hypercolumns away (in the input DataSpace's grid space) to allow proximal 
        // synapse connections from the corresponding input DataSpace. If set to -1 then there is no restriction 
		// and connections can form from any input value.
		/// </summary>
        public int Radius;

        public Input(DataSpace dataSpace, int radius)
        {
            this.DataSpace = dataSpace;
            this.Radius = radius;
        }
    }

	/// <summary>
    /// Represents an entire region of HTM columns for the CLA.
	/// <summary>
	/// <remarks>
    /// Code to represent an entire Hierarchical Temporal Memory (HTM) Region of 
    /// Columns that implement Numenta's new Cortical Learning Algorithms (CLA). 
    /// The Region is defined by a matrix of columns, each of which contains several cells.  
    /// The main idea is that given a matrix of input bits, the Region will first sparsify 
    /// the input such that only a few Columns will become 'active'.  As the input matrix 
    /// changes over time, different sets of Columns will become active in sequence.  
    /// The Cells inside the Columns will attempt to learn these temporal transitions and 
    /// eventually the Region will be able to make predictions about what may happen next
    /// given what has happened in the past. 
    /// For (much) more information, visit www.numenta.com.
    /// 
    /// 1) Start with an input consisting of a fixed number of bits. These bits might 
    ///    represent sensory data or they might come from another region lower in the 
    ///    hierarchy.
    /// 2) Assign a fixed number of columns to the region receiving this input. Each 
    ///    column has an associated dendrite segmentUpdateList. Each dendrite 
    ///    segmentUpdateList has a set of potential synapses representing a subset of the 
    ///    input bits. Each potential synapse has a permanence value.
    ///    Based on their permanence values, some of the potential synapses will be valid.
    /// 3) For any given input, determine how many valid synapses on each column are 
    ///    connected to active input bits.
    /// 4) The number of active synapses is multiplied by a 'boosting' factor which is 
    ///    dynamically determined by how often a column is active relative to its neighbors. 
    /// 5) The columns with the highest activations after boosting disable all but a fixed 
    ///    percentage of the columns within an inhibition radius. The inhibition radius is 
    ///    itself dynamically determined by the spread (or 'fan-out') of input bits. 
    ///    There is now a sparse set of active columns.
    /// 6) For each of the active columns, we adjust the permanence values of all the 
    ///    potential synapses. The permanence values of synapses aligned with active input 
    ///    bits are increased. The permanence values of synapses aligned with inactive 
    ///    input bits are decreased. The changes made to permanence values may change 
	///    some synapses from being valid to not valid, and vice-versa.
	/// </remarks>
    public class Region : DataSpace
    {
		/// <summary>
		/// List of all columns.
		/// </summary>
        public Column[] Columns;

		/// <summary>
		/// The number of context learning cells per column.
		/// </summary>
        public int CellsPerCol;

		/// <summary>
        /// If set to true, the Region will assume the Column
        /// grid dimensions exactly matches that of the input data.
        /// 
        /// The hardcoded flag is something added to bypass the spatial 
        /// pooler for exclusively working with the temporal sequences. It is more than
        /// just disabling spatial learning. In this case the Region will assume the 
        /// column grid dimensions exactly matches that of the input data. Then on each
        /// time step no spatial pooling is performed, instead we assume the input data
        /// is itself describing the state of the active columns. Think of hardcoded as
        /// though the spatial pooling has already been performed outside the network and
		/// we are just passing in the results to the columns as-is.
		/// </summary>
        public bool HardcodedSpatial;

		/// <summary>
		/// Determines whether InhibitionRadius uses a set given radius, or is automatcially determined based on average receptive field size.
		/// </summary>
        private InhibitionType _inhibitionType;

		/// <summary>
		/// This radius determines how many hypercolumns in a local area are considered during
		/// spatial pooling inhibition.
		/// </summary>
        public float InhibitionRadius;

		/// <summary>
		/// Furthest number of columns away (in this Region's Column grid space) to allow new distal 
        /// synapse connections.  If set to 0 then there is no restriction and connections
		/// can form between any two columns in the region.
		/// </summary>
        public int PredictionRadius;

		/// <summary>
        /// The number of new distal synapses added to segments if no matching ones are found 
		/// during learning.
		/// </summary>
		public int NewSynapsesCount;

		/// <summary>
		/// Activation threshold for a segment.
		/// If the number of active connected synapses in a segment is greater than activation threshold, the segment is said to be active.
		/// </summary>
		public int SegActiveThreshold;

		/// <summary>
		/// This parameter determines whether a segment will be considered a match to activity. It may be considered a match if at least
		/// this number of the segment's synapses match the actvity. The segment will then be re-used to represent that activity, with new syanpses
		/// added to fill out the pattern. The lower this number, the more patterns will be added to a single segment, which can be very bad because
		/// the same cell is thus used to represent an input in diffrent contexts, and also if the ratio of PermanenceInc to PermanenceDec
		/// is such that multiple patterns cannot be supported on one synapse, so all but 1 will generally remain disconnected, so predictions are never made.
		/// These are the minimum and maximum values of a range. Each individual column is given a different MinOverlapToReuseSegment value from within this range.
		/// </summary>
        public int MinMinOverlapToReuseSegment, MaxMinOverlapToReuseSegment;

		/// <summary>
		/// Percent of input bits each Column will have potential proximal (spatial) 
		/// synapses for.
		/// </summary>
        public float PctInputPerColumn;

		/// <summary>
		/// Minimum percent of column's proximal synapses that must be active for column 
        /// to be considered during spatial pooling inhibition.  This value helps set the
		/// minimum column overlap value during region run.
		/// </summary>
        public float PctMinOverlap;

		/// <summary>
		/// Approximate percent of Columns within average receptive field radius to be 
		/// winners after spatial pooling inhibition.
		/// </summary>
        public float PctLocalActivity;

		/// <summary>
		// The maximum allowed Boost value. Setting a max Boost value prevents competition
		// for activation from continuing indefinitely, and so facilitates stabilization of representations.
		/// </summary>
        public float MaxBoost;

		/// <summary>
		/// The amount that is added to a Column's Boost value in a single time step, when it is being boosted.
		/// </summary>
        public float BoostRate;

		/// <summary>
		/// Start and end times for spatial learning, temporal learning, and boosting periods. 
		/// -1 for start time means start from beginning; -1 for end time means continue through end. 
		/// 0 for end time means no learning of that type will take place (since time clock starts at 1).
		/// </summary>
        public int SpatialLearningStartTime;
        public int SpatialLearningEndTime;
        public int TemporalLearningStartTime;
        public int TemporalLearningEndTime;
        public int BoostingStartTime;
        public int BoostingEndTime;

		/// <summary>
		/// Whether this Region will output the activity of the column as a whole, and of each Cell individually.
		/// </summary>
        public bool OutputColumnActivity, OutputCellActivity;

		/// <summary>
		/// This Region's synapse parameters.
		/// </summary>
        public SynapseParams ProximalSynapseParams, DistalSynapseParams;

		/// <summary>
		/// A list of pointers to each of this Region's input DataSpaces and its input radius.
		/// </summary>
        public List<Input> InputList = new List<Input>();

		/// <summary>
		/// Increments at every time step.
		/// </summary>
		public int StepCounter;

		/// <summary>
        /// Initializes a new instance of the Region class.
		/// </summary>
		/// <remarks>
        /// Prior to receiving any inputs, the region is initialized by computing a list of 
        /// initial potential synapses for each column. This consists of a random set of 
        /// inputs selected from the input space. Each input is represented by a synapse 
        /// and assigned a random permanence value. The random permanence values are chosen 
        /// with two criteria. First, the values are chosen to be in a small range around 
        /// connectedPerm (the minimum permanence value at which a synapse is considered 
        /// "connected"). This enables potential synapses to become connected (or 
        /// disconnected) after a small number of training iterations. Second, each column 
        /// has a natural center over the input region, and the permanence values have a bias
        /// towards this center (they have higher values near the center).
        /// 
        /// In addition to this it was added a concept of Input Radius, which is an 
        /// additional parameter to control how far away synapse connections can be made 
        /// instead of allowing connections anywhere.  The reason for this is that in the
        /// case of video images I wanted to experiment with forcing each Column to only
        /// learn on a small section of the total input to more effectively learn lines or 
        /// corners in a small section without being 'distracted' by learning larger patterns
        /// in the overall input space (which hopefully higher hierarchical Regions would 
        /// handle more successfully).  Passing in -1 for input radius will mean no 
        /// restriction which will more closely follow the Numenta doc if desired.
		/// </remarks> 
		/// <param name="colGridSize">Point structure that represents the 
		///     region size in columns (ColumnSizeX * ColumnSizeY).
		/// </param>
		/// <param name="pctInputPerCol">The percent of input bits each column has potential
		///     synapses for (PctInputCol).
		/// </param>
		/// <param name="pctMinOverlap">The minimum percent of column's proximal synapses
		///     that must be active for a column to be even be considered during spatial 
		///     pooling.
		/// </param>
		/// <param name="predictionRadius">Furthest number of columns away to allow
		///     distal synapses from each column/cell.
		/// </param>
		/// <param name="pctLocalActivity">Approximate percent of Columns within inhibition
		///     radius to be winners after spatial inhibition.
		/// </param>
		/// <param name="maxBoost">The maximum allowed Boost value. Setting a max Boost value prevents 
		///     competition for activation from continuing indefinitely, and so 
		///     facilitates stabilization of representations.
		/// </param>
		/// <param name="boostPeriod">The time period in which Boosting may occur (-1 for no limit).
		/// </param>
		/// <param name="cellsPerCol">The number of context learning cells per column.
		/// </param>
		/// <param name="segActiveThreshold">The minimum number of synapses that must be 
		///     active for a segment to fire.
		/// </param>
		/// <param name="newSynapseCount">The number of new distal synapses added if
		///     no matching ones found during learning.
		/// </param>
		/// <param name="hardcodedSpatial">If set to true, this Region must have exactly one
		///     input, with the same dimensions as this Region. What is active in 
		///     that input will directly dictate what columns are activated in this
		///     Region, with the spatial pooling and inhibition steps skipped.
		/// </param>
		/// <param name="spatialLearning">If true, spatial learning takes place as normal.
		/// </param>
		/// <param name="temporalLearning">If true, temporal learning takes place as normal.
		/// </param>
		/// <param name="outputColumnActivity">If true, there will be one value in this Region's
		///     output representing activity of each column as a whole.
		/// </param>
		/// <param name="outputCellActivity">If true, there will be one value in this Region's
		///     output representaing activity of each cell in each column.
		/// </param>
        public Region(string id, Point colGridSize, int hypercolumnDiameter, SynapseParams proximalSynapseParams, SynapseParams distalSynapseParams, float pctInputPerCol, float pctMinOverlap, int predictionRadius, InhibitionType inhibitionType, int inhibitionRadius, float pctLocalActivity, float boostRate, float maxBoost, int spatialLearningStartTime, int spatialLearningEndTime, int temporalLearningStartTime, int temporalLearningEndTime, int boostingStartTime, int boostingEndTime, int cellsPerCol, int segActiveThreshold, int newSynapseCount, int minMinOverlapToReuseSegment, int maxMinOverlapToReuseSegment, bool hardcodedSpatial, bool outputColumnActivity, bool outputCellActivity) : base(id)
        {
            this.SizeX = colGridSize.X;
            this.SizeY = colGridSize.Y;
            this.HypercolumnDiameter = hypercolumnDiameter;
            this.ProximalSynapseParams = proximalSynapseParams;
            this.DistalSynapseParams = distalSynapseParams;
            this.PredictionRadius = predictionRadius;
            this._inhibitionType = inhibitionType;
            this.InhibitionRadius = inhibitionRadius;
            this.BoostRate = boostRate;
            this.MaxBoost = maxBoost;
            this.SpatialLearningStartTime = spatialLearningStartTime;
            this.SpatialLearningEndTime = spatialLearningEndTime;
            this.TemporalLearningStartTime = temporalLearningStartTime;
            this.TemporalLearningEndTime = temporalLearningEndTime;
            this.BoostingStartTime = boostingStartTime;
            this.BoostingEndTime = boostingEndTime;
            this.CellsPerCol = cellsPerCol;
            this.SegActiveThreshold = segActiveThreshold;
            this.NewSynapsesCount = newSynapseCount;
            this.MinMinOverlapToReuseSegment = minMinOverlapToReuseSegment;
            this.MaxMinOverlapToReuseSegment = maxMinOverlapToReuseSegment;
            this.PctLocalActivity = pctLocalActivity;
            this.PctInputPerColumn = pctInputPerCol;
            this.PctMinOverlap = pctMinOverlap;
            this.HardcodedSpatial = hardcodedSpatial;
            this.OutputColumnActivity = outputColumnActivity;
            this.OutputCellActivity = outputCellActivity;

            // The number of output values per column of this Region. If OutputCellActivity is true,
            // then this number will include the number of cells. If OutputColumnActivity is true, then
            // this number will include one value representing the activity of the column as a whole.
            this.SizeZ = (this.OutputColumnActivity
                              ? 1
                              : 0) + (this.OutputCellActivity
                                          ? this.CellsPerCol
                                          : 0);

			// Create the columns based on the size of the input data to connect to.
			var _random = new Random();
            this.Columns = new Column[this.SizeX * this.SizeY];
            for (int cy = 0; cy < this.SizeY; cy++)
            {
                for (int cx = 0; cx < this.SizeX; cx++)
                {
                    // Determine the current column's minOverlapToReuseSegment, a random value within this Region's range.
                    int minOverlapToReuseSegment = _random.Next() % (this.MaxMinOverlapToReuseSegment - this.MinMinOverlapToReuseSegment + 1) + this.MinMinOverlapToReuseSegment;

                    // Create a column with sourceCoords and GridCoords
                    this.Columns[(cy * this.SizeX) + cx] = new Column(this, new Point(cx, cy), minOverlapToReuseSegment);
                }
            }
        }

		/// <summary>
		/// Called after adding all inputs to this Region.
		/// </summary>
        public void Initialize()
        {
            if (this.HardcodedSpatial == false)
            {
                // Create Segments with potential synapses for columns
                for (int colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
                {
                    this.Columns[colIndex].CreateProximalSegments();
                }

                if (this._inhibitionType == InhibitionType.Automatic)
                {
                    // Initialize InhibitionRadius based on the average receptive field size.
                    this.InhibitionRadius = this.AverageReceptiveFieldSize();
                }

                // Determine the DesiredLocalActivity value for each Column, based on InhibitionRadius.
                for (int colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
                {
                    this.Columns[colIndex].DetermineDesiredLocalActivity();
                }
            }
        }

		/// <summary>
		/// Performs spatial pooling for the current input in this Region.
		/// </summary>
		/// <remarks>
        /// The result will be a subset of Columns being set as active as well
        /// as (proximal) synapses in all Columns having updated permanences and boosts, and 
        /// the Region will update inhibitionRadius.
		/// 
        /// From the Numenta white paper:
        /// Phase 1: 
        ///     Compute the overlap with the current input for each column. Given an input 
        ///     vector, the first phase calculates the overlap of each column with that 
        ///     vector. The overlap for each column is simply the number of connected 
        ///     synapses with active inputs, multiplied by its boost. If this value is 
        ///     below minOverlap, we set the overlap score to zero.
        /// Phase 2: 
        ///     Compute the winning columns after inhibition. The second phase calculates
        ///     which columns remain as winners after the inhibition step. 
        ///     desiredLocalActivity is a parameter that controls the number of columns 
        ///     that end up winning. For example, if desiredLocalActivity is 10, a column 
        ///     will be a winner if its overlap score is greater than the score of the 
        ///     10'th highest column within its inhibition radius.
        /// Phase 3: 
        ///     Update synapse permanence and internal variables.The third phase performs 
        ///     learning; it updates the permanence values of all synapses as necessary, 
        ///     as well as the boost and inhibition radius. The main learning rule is 
        ///     implemented in lines 20-26. For winning columns, if a synapse is active, 
        ///     its permanence value is incremented, otherwise it is decremented. Permanence 
        ///     values are constrained to be between 0 and 1.
        ///     Lines 28-36 implement boosting. There are two separate boosting mechanisms 
        ///     in place to help a column learn connections. If a column does not win often 
        ///     enough (as measured by activeDutyCycle), its overall boost value is 
        ///     increased (line 30-32). Alternatively, if a column's connected synapses do 
        ///     not overlap well with any inputs often enough (as measured by 
        ///     overlapDutyCycle), its permanence values are boosted (line 34-36). 
        /// Note: Once learning is turned off, boost(c) is frozen. 
		/// Finally at the end of Phase 3 the inhibition radius is recomputed (line 38).
		/// </remarks>
        public void PerformSpatialPooling()
        {
            int colIndex;
            Column column;

            if (this.HardcodedSpatial)
            {
                // This Region should have at most one DataSpace, which should be the same size as this Region and have one value per column.
                DataSpace inputDataSpace = this.InputList[0].DataSpace;
                if (inputDataSpace != null && (inputDataSpace.SizeX == this.SizeX) && (inputDataSpace.SizeY == this.SizeY))
                {
                    for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
                    {
                        column = this.Columns[colIndex];
                        column.IsActive = inputDataSpace.GetIsActive(column.Position.X, column.Position.Y, 0);
                    }
                }
                return;
            }

            // Determine whether spatial learning is currently allowed.
            bool allowSpatialLearning = Global.AllowSpatialLearning && ((this.SpatialLearningStartTime == -1) || (this.SpatialLearningStartTime <= this.StepCounter)) && ((this.SpatialLearningEndTime == -1) || (this.SpatialLearningEndTime >= this.StepCounter));

            // Determine whether boosting is currently allowed.
            bool allowBoosting = Global.AllowBoosting && ((this.BoostingStartTime == -1) || (this.BoostingStartTime <= this.StepCounter)) && ((this.BoostingEndTime == -1) || (this.BoostingEndTime >= this.StepCounter));

            // Phase 1: Compute Input Overlap
            for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
            {
                this.Columns[colIndex].ComputeOverlap();
            }

            // Phase 2: Compute active columns (Winners after inhibition)
            for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
            {
                this.Columns[colIndex].ComputeColumnInhibition();
            }

            // Phase 3: Synapse Learning and Determining Boosting
            for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
            {
                column = this.Columns[colIndex];

                if (allowSpatialLearning && column.IsActive)
                {
                    column.AdaptPermanences();
                }

                column.UpdateDutyCycles();

                if (allowBoosting)
                {
                    column.PerformBoosting();
                }
            }

            if (allowSpatialLearning && (this._inhibitionType == InhibitionType.Automatic))
            {
                // Determine the new InhibitionRadius value based on average receptive field size.
                this.InhibitionRadius = this.AverageReceptiveFieldSize();

                // Determine the new DesiredLocalActivity value for each column.
                for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
                {
                    this.Columns[colIndex].DetermineDesiredLocalActivity();
                }
            }
        }

		/// <summary>
		/// Performs temporal pooling based on the current spatial pooler output.
		/// </summary>
		/// <remarks>
        /// From the Numenta white paper:
        /// The input to this code is activeColumns(t), as computed by the spatial pooler. 
        /// The code computes the active and predictive state for each cell at the current 
        /// timestep, t. The boolean OR of the active and predictive states for each cell 
        /// forms the output of the temporal pooler for the next level.
        /// Phase 1: 
		///     Compute the active state, activeState(t), for each cell.
		///     The first phase calculates the activeState for each cell that is in a winning 
		///     column. For those columns, the code further selects one cell per column as the 
		///     learning cell (learnState). The logic is as follows: if the bottom-up input was 
		///     predicted by any cell (i.e. its predictiveState output was 1 due to a sequence 
		///     segmentUpdateList), then those cells become active (lines 23-27). 
		///     If that segmentUpdateList became active from cells chosen with learnState on, 
		///     this cell is selected as the learning cell (lines 28-30). If the bottom-up input 
		///     was not predicted, then all cells in the column become active (lines 32-34). 
		///     In addition, the best matching cell is chosen as the learning cell (lines 36-41) 
		///     and a new segmentUpdateList is added to that cell.
        /// Phase 2:
		///     Compute the predicted state, predictiveState(t), for each cell.
		///     The second phase calculates the predictive state for each cell. A cell will turn 
		///     on its predictive state output if one of its segments becomes active, i.e. if 
		///     enough of its lateral inputs are currently active due to feed-forward input. 
		///     In this case, the cell queues up the following changes: a) reinforcement of the 
		///     currently active segmentUpdateList (lines 47-48), and b) reinforcement of a 
		///     segmentUpdateList that could have predicted this activation, i.e. a 
		///     segmentUpdateList that has a (potentially weak) match to activity during the 
		///     previous time step (lines 50-53).
        /// Phase 3:
		///     Update synapses. The third and last phase actually carries out learning. In this 
		///     phase segmentUpdateList updates that have been queued up are actually implemented 
		///     once we get feed-forward input and the cell is chosen as a learning cell 
		///     (lines 56-57). Otherwise, if the cell ever stops predicting for any reason, we 
		///     negatively reinforce the segments (lines 58-60).
		/// </remarks>
        public void PerformTemporalPooling()
        {
            int colIndex;
            Column column;
            Cell cell;

            // Determine whether temporal learning is currently allowed.
            bool allowTemporalLearning = Global.AllowTemporalLearning && ((this.TemporalLearningStartTime == -1) || (this.TemporalLearningStartTime <= this.StepCounter)) && ((this.TemporalLearningEndTime == -1) || (this.TemporalLearningEndTime >= this.StepCounter));

            // Phase 1: Compute cell active states and segment learning updates
            for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
            {
                column = this.Columns[colIndex];

                if (column.IsActive)
                {
                    bool predicted = false;
                    bool learnCellChosen = false;

                    for (int cellIndex = 0; cellIndex < this.CellsPerCol; cellIndex++)
                    {
                        cell = column.Cells[cellIndex];

                        if (cell.WasPredicted)
                        {
                            Segment segment = cell.GetPreviousActiveSegment();
                            if ((segment != null) && segment.IsSequence)
                            {
                                predicted = true;
                                cell.IsActive = true;
                                if (allowTemporalLearning && segment.GetWasActiveFromLearning())
                                {
                                    learnCellChosen = true;
                                    cell.IsLearning = true;
                                }
                            }
                        }
                    }

                    if (!predicted)
                    {
                        for (int cellIndex = 0; cellIndex < this.CellsPerCol; cellIndex++)
                        {
                            cell = column.Cells[cellIndex];
                            cell.IsActive = true;
                        }
                    }

                    if (allowTemporalLearning && !learnCellChosen)
                    {
                        // isSequence=true, previous=true
                        Cell bestCell = null;
                        Segment bestSegment = null;
                        column.GetBestMatchingCell(1, true, out bestCell, out bestSegment);
						if (Global.Debug)
						{
							Debug.Assert (bestCell != null);
						}

                        bestCell.IsLearning = true;

                        // segmentToUpdate is added internally to the bestCell's update list.
                        // Then set number of prediction steps to 1 (meaning it's a sequence segment)
                        SegmentUpdateInfo segmentUpdateInfo = bestCell.UpdateSegmentActiveSynapses(true, bestSegment, true, UpdateType.DueToActive);
                        segmentUpdateInfo.NumPredictionSteps = 1;
                    }
                }
            }

			// Phase 2: Compute the predicted state for each cell
            for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
            {
                column = this.Columns[colIndex];

                for (int cellIndex = 0; cellIndex < this.CellsPerCol; cellIndex++)
                {
                    cell = column.Cells[cellIndex];

                    // Process all segments on the cell to cache the activity for later.
                    foreach (Segment seg in cell.Segments)
                    {
                        seg.ProcessSegment();
                    }

                    foreach (Segment seg in cell.Segments)
                    {
                        // Now check for an active segment, we only need one for the cell to predict, but all Segments need to be checked
                        // so that a segment update will be created for each active segment, and so that the lowest numPredictionSteps 
                        // among active segments is adopted by the cell.
                        if (seg.IsActive)
                        {
                            cell.SetIsPredicting(true, seg.NumPredictionSteps);

                            if (seg.IsSequence)
                            {
                                cell.IsSegmentPredicting = true;
                            }

                            // a) reinforcement of the currently active segments
                            if (allowTemporalLearning)
                            {
                                // Add segment update to this cell
                                cell.UpdateSegmentActiveSynapses(false, seg, false, UpdateType.DueToPredictive);
                            }
                        }
                    }

                    // b) reinforcement of a segment that could have predicted 
                    //    this activation, i.e. a segment that has a (potentially weak)
                    //    match to activity during the previous time step (lines 50-53).
                    // NOTE: The check against MaxTimeSteps is a correctly functioning way of enforcing a maximum number of time steps, 
                    // as opposed to the previous way of storing Max(numPredictionSteps, MaxTimeSteps) as a segment's numPredictionSteps,
                    // which caused inaccurate numPredictionSteps values to be stored, resulting in duplicate segments being created.
                    // Note also that if the system of recording and using an exact number of time steps is abandonded (and replaced with the
                    // original sequence/non-sequence system), then all references to MaxTimeSteps can be removed.
                    if (allowTemporalLearning && cell.IsPredicting && (cell.NumPredictionSteps != Segment.MaxTimeSteps))
                    {
                        Segment predictiveSegment = cell.GetBestMatchingPreviousSegment();

                        // Either update existing or add new segment for this cell considering
                        // only segments matching the number of prediction steps of the
                        // best currently active segment for this cell.
                        SegmentUpdateInfo predictiveSegUpdate = cell.UpdateSegmentActiveSynapses(true, predictiveSegment, true, UpdateType.DueToPredictive);
                        if (predictiveSegment == null)
                        {
                            predictiveSegUpdate.NumPredictionSteps = cell.NumPredictionSteps + 1;
                        }
                    }
                }
            }

			// Phase 3: Update synapses
            if (allowTemporalLearning)
            {
                for (colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
                {
                    column = this.Columns[colIndex];

                    for (int cellIndex = 0; cellIndex < this.CellsPerCol; cellIndex++)
                    {
                        cell = column.Cells[cellIndex];

                        if (cell.IsLearning)
                        {
                            cell.ApplySegmentUpdates(this.StepCounter, ApplyUpdateTrigger.Active);
                        }
                        else if ((cell.IsPredicting == false) && cell.WasPredicted)
                        {
                            cell.ApplySegmentUpdates(this.StepCounter, ApplyUpdateTrigger.Inactive);
                        }
                        else if (cell.IsPredicting && cell.WasPredicted && (cell.NumPredictionSteps > 1) && (cell.PrevNumPredictionSteps == 1))
                        {
                            cell.ApplySegmentUpdates(this.StepCounter, ApplyUpdateTrigger.LongerPrediction);
                        }
                    }
                }
            }
        }

		/// <summary>
		/// Get a reference to the Column at the specified column grid coordinate.
		/// </summary>
		/// <param name="x">the x coordinate component of the column's position.</param>
		/// <param name="y">the y coordinate component of the column's position.</param>
		/// <returns>a pointer to the Column at that position.</returns>
        public Column GetColumn(int x, int y)
        {
            return this.Columns[(y * this.SizeX) + x];
        }

		/// <summary>
		/// The radius of the average connected receptive field size of all the columns.
		/// </summary>
		/// <remarks>
        /// The connected receptive field size of a column includes only the connected 
        /// synapses (those with permanence values >= connectedPerm). This is used to 
		/// determine the extent of lateral inhibition between columns.
		/// </remarks>
		/// <returns>The average connected receptive field size (in hypercolumn grid space).</returns>
        public float AverageReceptiveFieldSize()
        {
            float sum = 0.0f;

            for (int colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
            {
                Column column = this.Columns[colIndex];

                // Initialize maxIstance to 0.
                float maxDistance = 0.0f;

                // Iterate through each of the current column's proximal synapses...
                foreach (Synapse syn in column.ProximalSegment.Synapses)
                {
                    // Skip non-connected synapses.
                    if (syn.IsConnected == false)
                    {
                        continue;
                    }

                    // Determine the distance of the further proximal synapse. This will be considered the size of the receptive field.
                    maxDistance = Math.Max(maxDistance, ((ProximalSynapse)syn).DistanceToInput);
                }

                // Add the current column's receptive field size to the sum.
                sum += maxDistance;
            }

            // Return this Region's average receptive field size.
            return (sum / (this.SizeX * this.SizeY));
        }

		/// <summary>
		/// Return true if the given Column has an overlap value that is at least the
		/// k'th largest amongst all neighboring columns within inhibitionRadius hypercolumns around the given Column's hypercolumn.
		/// </summary>
		/// <remarks>
        /// This function is effectively determining which columns are to be inhibited 
		/// during the spatial pooling procedure of the region.
		/// </remarks>
        public bool IsWithinKthScore(Column column, int k)
        {
            // Determine the extent, in columns, of the area within the InhibitionRadius hypercolumns of the given column's hypercolumn.
            Area inhibitionArea = column.DetermineColumnsWithinHypercolumnRadius((int)(this.InhibitionRadius + 0.5f));

            //Loop over all columns that are within inhibitionRadius hypercolumns of the given column's hypercolumn.
            // Count how many neighbor columns have strictly greater overlap than our
            // given column. If this count is <k then we are within the kth score
            int numberColumns = 0;
            for (int x = inhibitionArea.MinX; x <= inhibitionArea.MaxX; x++)
            {
                for (int y = inhibitionArea.MinY; y <= inhibitionArea.MaxY; y++)
                {
                    numberColumns += (this.Columns[(y * this.SizeX) + x].Overlap > column.Overlap)
                                         ? 1
                                         : 0;
                }
            }
            return numberColumns < k; //if count is < k, we are within the kth score of all neighbors
        }

		/// <summary>
		/// Run one time step iteration for this Region.
		/// </summary>
		/// <remarks>
        /// All cells will have their current (last run) state pushed back to be their 
        /// new previous state and their new current state reset to no activity.  
		/// Then SpatialPooling followed by TemporalPooling is performed for one time step.
		/// </remarks>
        public override void NextTimeStep()
        {
            foreach (Input input in this.InputList)
            {
                input.DataSpace.NextTimeStep();
            }

            for (int colIndex = 0; colIndex < this.SizeX * this.SizeY; colIndex++)
            {
                Column column = this.Columns[colIndex];

                column.ProximalSegment.NextTimeStep();

                for (int cellIndex = 0; cellIndex < this.CellsPerCol; cellIndex++)
                {
                    Cell cell = column.Cells[cellIndex];
                    cell.NextTimeStep();
                }
            }

            // Compute Region statistics
			StepCounter++;    

            // Perform pooling
            this.PerformSpatialPooling();
            this.PerformTemporalPooling();
        }

		#region DataSpace Methods

		public override DataSpaceType GetDataSpaceType()
		{
			return DataSpaceType.Region;
		}

		public override bool GetIsActive(int x, int y, int z)
		{
			if (Global.Debug)
			{
				Debug.Assert ((x >= 0) && (x < this.SizeX));
				Debug.Assert ((y >= 0) && (y < this.SizeY));
				Debug.Assert ((z >= 0) && (z < this.SizeZ));
			}

			Column column = this.Columns[x + (y * this.SizeX)];

			if (this.OutputColumnActivity && (z == (this.SizeZ - 1)))
			{
				// Return value representing activity of the entire column x, y.
				// NOTE: If this will ever be used, should it also return true if any cell in the column is in predictive state?
				return column.IsActive;
			}
			else
			{
				// Return value representing activity of the column's cell with the given z (active or predictive state).
				Cell cell = column.Cells[z];
				return cell.IsActive || cell.IsPredicting;
			}
		}

		/// <summary>
		/// Add an input (i.e. a lower region or sensor) to this Region
		/// </summary>
		public void AddInput(DataSpace inputDataSpace, int inputRadius)
		{
			// If HardcodedSpatial is true, then there can be only one input DataSpace, and it must be the same size as this Region, and have only 1 value.
			if (Global.Debug)
			{
				Debug.Assert ((this.HardcodedSpatial == false) || ((this.InputList.Count == 0) && (inputDataSpace.SizeZ == 1) && (inputDataSpace.SizeX == this.SizeX) && (inputDataSpace.SizeY == this.SizeY)));
			}

			this.InputList.Add(new Input(inputDataSpace, inputRadius));
		}

		/// <summary>
		/// Determine whether a particular cell in this Region is active.
		/// </summary>
		public bool IsCellActive(int x, int y, int z)
		{
			if (Global.Debug)
			{
				Debug.Assert ((x >= 0) && (x < this.SizeX));
				Debug.Assert ((y >= 0) && (y < this.SizeY));
			}

			Column column = this.Columns[x + (y * this.SizeX)];

			// Return value representing activity of the column's cell with the given z.
			return column.Cells[z].IsActive;
		}

		/// <summary>
		/// Determine whether a particular cell in this Region is predicted.
		/// </summary>
		public bool IsCellPredicted(int x, int y, int z)
		{
			if (Global.Debug)
			{
				Debug.Assert ((x >= 0) && (x < this.SizeX));
				Debug.Assert ((y >= 0) && (y < this.SizeY));
			}

			Column column = this.Columns[x + (y * this.SizeX)];

			// Return value representing prediction of the column's cell with the given z.
			return column.Cells[z].IsPredicting;
		}

		/// <summary>
		/// Determine whether a particular cell in this Region is learning.
		/// </summary>
		public bool IsCellLearning(int x, int y, int z)
		{
			if (Global.Debug)
			{
				Debug.Assert ((x >= 0) && (x < this.SizeX));
				Debug.Assert ((y >= 0) && (y < this.SizeY));
			}

			Column column = this.Columns[x + (y * this.SizeX)];

			// Return value representing prediction of the column's cell with the given z.
			return column.Cells[z].IsLearning;
		}

		public Cell GetCell(int x, int y, int z)
		{
			if (Global.Debug)
			{
				Debug.Assert ((x >= 0) && (x < this.SizeX));
				Debug.Assert ((y >= 0) && (y < this.SizeY));
			}

			Column column = this.Columns[x + (y * this.SizeX)];

			// Return pointer to the given column's cell with the given z.
			return column.Cells[z];
		}

		#endregion
    }
}
