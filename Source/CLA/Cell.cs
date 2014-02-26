using System;
using System.Collections.Generic;
using System.Linq;

namespace CLA
{
	/// <summary>
	/// A data structure representing a single context sensitive cell.
	/// </summary>
    public class Cell
    {
        public Column Column;

        public int NumPredictionSteps;
        public int PrevNumPredictionSteps;

        public List<Segment> Segments = new List<Segment>();
        private List<SegmentUpdateInfo> _segmentUpdates = new List<SegmentUpdateInfo>();

		/// <summary>
		/// Position in Column.
		/// </summary>
        public int Index;

		/// <summary>
        /// Gets or sets a value indicating whether this Cell is active.
		/// true if this instance is active; otherwise, false.
		/// </summary>
        public bool IsActive
        {
            get
            {
                return this._isActive;
            }
            set
            {
                this._isActive = value;

                // Record this cells's latest time being active.
                if (this._isActive)
                {
                    this.PrevActiveTime = this.Column.Region.StepCounter;
                }
            }
        }

        private bool _isActive;

		/// <summary>
        /// Gets a value indicating whether this Cell was active.
		/// true if it was active; otherwise, false.
		/// </summary>
        public bool WasActive;

		/// <summary>
        /// Gets or sets a value indicating whether this Cell is learning.
		/// true if it is learning; otherwise, false.
		/// </summary>
        public bool IsLearning;

		/// <summary>
        /// Gets a value indicating whether this Cell was learning.
		/// true if it was learning; otherwise, false.
		/// </summary>
        public bool WasLearning;

        public bool IsPredicting;

        public bool IsSegmentPredicting;

		/// <summary>
		/// Indicates whether this cell was predicted to become active.
		/// </summary>
        public bool WasPredicted;

        public int PrevActiveTime;

        public Cell()
        {
        }

        public void SetIsPredicting(bool value, int numPredictionSteps)
        {
            if (value && !this.IsPredicting)
            {
                // This call is for the first segment found active, to make this cell predictive. Adopt that segment's NumPredictionSteps.
                this.NumPredictionSteps = numPredictionSteps;
            }
            else
            {
                // This call is not for the firt active segment found; use the lower NumPredictionSteps between the segment's given value and the stored value.
                this.NumPredictionSteps = Math.Min(this.NumPredictionSteps, numPredictionSteps);
            }

            this.IsPredicting = value;
        }

		/// <summary>
        /// Initialize a new Cell belonging to the specified Column. The index (Z) is an 
		/// integer id to distinguish this Cell from others in the Column.
		/// </summary>
        public void Initialize(Column column, int index)
        {
            this.Column = column;
            this.Index = index;
            this.IsActive = false;
            this.WasActive = false;
            this.IsPredicting = false;
            this.WasPredicted = false;
            this.IsLearning = false;
            this.WasLearning = false;
            this.NumPredictionSteps = 0;
            this.PrevNumPredictionSteps = 0;
            this.PrevActiveTime = -1;
        }

		/// <summary>
		/// Advances this cell to the next time step.
		/// </summary>
		/// <remarks>
        /// The current state of this cell (active, learning, predicting) will be set as the
        /// previous state and the current state will be reset to no cell activity by 
		/// default until it can be determined.
		/// </remarks>
        public void NextTimeStep()
        {
            this.WasPredicted = this.IsPredicting;
            this.WasActive = this.IsActive;
            this.WasLearning = this.IsLearning;
            this.IsPredicting = false;
            this.IsSegmentPredicting = false;
            this.IsActive = false;
            this.IsLearning = false;
            this.PrevNumPredictionSteps = this.NumPredictionSteps;
            this.NumPredictionSteps = 0;

            foreach (Segment seg in this.Segments)
            {
                seg.NextTimeStep();
            }
        }

		/// <summary>
		/// Creates a new segment for this Cell.
		/// </summary>
		/// <remarks>
        /// The new segment will initially connect to at most newSynapseCount 
        /// synapses randomly selected from the set of cells that
		/// were in the learning state at t-1 (specified by the learningCells parameter).
		/// </remarks>
		/// <param name="learningCells">A set of available learning cells to add to the segmentUpdateList.</param>
		/// <returns>Created segment.</returns>
        public Segment CreateSegment(ref List<Cell> learningCells, int creationTime)
        {
            var newSegment = new Segment();
            newSegment.Initialize(creationTime, this.Column.Region.SegActiveThreshold);
            newSegment.CreateSynapsesToLearningCells(ref learningCells, this.Column.Region.DistalSynapseParams);
            this.Segments.Add(newSegment);
            return newSegment;
        }

		/// <summary>
        /// For this cell, return a Segment that was active in the previous
        /// time step. If multiple segments were active, sequence segments are given
		/// preference. Otherwise, segments with most activity are given preference.
		/// </summary>
        public Segment GetPreviousActiveSegment()
        {
            Segment bestSegment = null;
            bool foundSequence = false;
            int mostSyns = 0;

            foreach (Segment seg in this.Segments)
            {
                int activeSyns = seg.PrevActiveConnectedSynapsesCount;
                if (activeSyns >= seg.ActiveThreshold)
                {
                    //if segment is active, check for sequence segment and compare
                    //active synapses
                    if (seg.IsSequence)
                    {
                        foundSequence = true;
                        if (activeSyns > mostSyns)
                        {
                            mostSyns = activeSyns;
                            bestSegment = seg;
                        }
                    }
                    else if ((!foundSequence) && (activeSyns > mostSyns))
                    {
                        mostSyns = activeSyns;
                        bestSegment = seg;
                    }
                }
            }

            return bestSegment;
        }

		/// <summary>
        /// Add a new SegmentUpdateInfo object to this Cell containing proposed changes to the 
		/// specified segment. 
		/// </summary>
		/// <remarks>
        /// If the segment is NULL, then a new segment is to be added, otherwise
        /// the specified segment is updated.  If the segment exists, find all active
        /// synapses for the segment (either at t or t-1 based on the 'previous' parameter)
        /// and mark them as needing to be updated.  If newSynapses is true, then
        /// Region.newSynapseCount - len(activeSynapses) new synapses are added to the
        /// segment to be updated.  The (new) synapses are randomly chosen from the set
        /// of current learning cells (within Region.predictionRadius if set).
        ///
        /// These segment updates are only applied when the applySegmentUpdates
		/// method is later called on this Cell.
		/// </remarks>
        public SegmentUpdateInfo UpdateSegmentActiveSynapses(bool previous, Segment segment, bool newSynapses, UpdateType updateType)
        {
            List<DistalSynapse> activeDistalSyns = null;
            if (segment != null)
            {
                List<Synapse> activeSyns = previous
                                               ? segment.PrevActiveSynapses
                                               : segment.ActiveSynapses;
                activeDistalSyns = activeSyns.Cast<DistalSynapse>().ToList();
            }

            var segmentUpdate = new SegmentUpdateInfo();
            segmentUpdate.Initialize(this, segment, activeDistalSyns, newSynapses, this.Column.Region.StepCounter, updateType);
            this._segmentUpdates.Add(segmentUpdate);
            return segmentUpdate;
        }

		/// <summary>
		/// This function reinforces each segment in this Cell's SegmentUpdateInfo.
		/// </summary>
		/// <remarks>
        /// Using the segmentUpdateInfo, the following changes are
        /// performed. If positiveReinforcement is true then synapses on the active
        /// list get their permanence counts incremented by permanenceInc. All other
        /// synapses get their permanence counts decremented by permanenceDec. If
        /// positiveReinforcement is false, then synapses on the active list get
        /// their permanence counts decremented by permanenceDec. After this step,
        /// any synapses in segmentUpdate that do yet exist get added with a permanence
        /// count of initialPerm. These new synapses are randomly chosen from the
		/// set of all cells that have learnState output = 1 at time step t.
		/// </remarks>
        public void ApplySegmentUpdates(int curTime, ApplyUpdateTrigger trigger)
        {
            Segment segment = null;
            var modifiedSegments = new List<Segment>();

            // Iterate through all segment updates, skipping those not to be applied now, and removing those that are applied.
            int segInfoIndex = 0;
            while (segInfoIndex < this._segmentUpdates.Count)
            {
                SegmentUpdateInfo segInfo = this._segmentUpdates[segInfoIndex];

                // Do not apply the current segment update if it was created at the current time step, and was created as a result of the cell being predictive.
                // If this is the case, then this segment update can only be evaluated at a later time step, and so should remain in the queue for now.
                bool applyUpdate = !((segInfo.CreationTimeStep == curTime) && (segInfo.UpdateType == UpdateType.DueToPredictive));

                // Do not apply the current segment update if its numPredictionSteps > 1, and if we are applying updates due to the cell still being predictive,
                // but with a greater numPredictionSteps. Unless the segment being updated predicted activation in 1 prediction step, it cannot be proven
                // incorrect, and so shouldn't be processed yet. This is because a "prediction step" can take more than one time step.
                if ((trigger == ApplyUpdateTrigger.LongerPrediction) && (segInfo.NumPredictionSteps > 1))
                {
                    applyUpdate = false;
                }

                if (applyUpdate)
                {
                    segment = segInfo.Segment;

                    if (segment != null)
                    {
                        if (trigger == ApplyUpdateTrigger.Active)
                        {
                            // The cell has become actve; positively reinforce the segment.
                            segment.UpdatePermanences(ref segInfo.ActiveDistalSynapses);
                        }
                        else
                        {
                            // The cell has become inactive of is predictive but with a longer prediction. Negatively reinforce the segment.
                            segment.DecreasePermanences(ref segInfo.ActiveDistalSynapses);
                        }

                        // Record that this segment has been modified, so it can later be checked for synapses that should be removed.
                        if (modifiedSegments.Contains(segment) == false)
                        {
                            modifiedSegments.Add(segment);
                        }
                    }

                    // Add new synapses (and new segment if necessary)
                    if (segInfo.AddNewSynapses && (trigger == ApplyUpdateTrigger.Active))
                    {
                        if (segment == null)
                        {
                            if (segInfo.CellsThatWillLearn.Count > 0) //only add if learning cells available
                            {
                                segment = segInfo.CreateCellSegment(this.Column.Region.StepCounter);
                            }
                        }
                        else if (segInfo.CellsThatWillLearn.Count > 0)
                        {
                            //add new synapses to existing segment
                            segInfo.CreateSynapsesToLearningCells(this.Column.Region.DistalSynapseParams);
                        }
                    }

                    // Remove and release the current SegmentUpdateInfo.
                    this._segmentUpdates.RemoveAt(segInfoIndex);
                }
                else
                {
                    segInfoIndex++;
                }
            }

            // Only process modified segments if all segment updates have been processed, to avoid deleting segments or synapses that
            // are referred to by still-existing segment updates.
            if (this._segmentUpdates.Count == 0)
            {
                // All segment updates have been processed, so there are none left that may have references to this cell's 
                // synapses or segments. Therefore we can iterate through all segments modified above, to prune unneeded synpses and segments.
                while (modifiedSegments.Count > 0)
                {
                    // Get pointer to the current modified segment, and remove it from the modifiedSegments list.
                    segment = modifiedSegments[0];
                    modifiedSegments.RemoveAt(0);

                    // Remove from the current modified segment any synapses that have reached permanence of 0.
                    int synIndex = 0;
                    while (synIndex < segment.Synapses.Count)
                    {
                        Synapse syn = segment.Synapses[synIndex];
                        if (syn.Permanence == 0.0f)
                        {
                            // Remove and release the current synapse, whose permanence has reached 0.
                            segment.Synapses.RemoveAt(synIndex);
                        }
                        else
                        {
                            synIndex++;
                        }
                    }

                    // If this modified segment now has no synapses, remove the segment from this cell.
                    if (segment.Synapses.Count == 0)
                    {
                        this.Segments.Remove(segment);
                    }
                }
            }
            else
            {
                // Not all of the segment updates have been processed, so synapses and segments cannot be pruned. 
                // (because they may be referred to by segment updates that still exist). So just clear the list of modified segments.
                modifiedSegments.Clear();
            }
        }

		/// <summary>
        /// For this cell in the previous time step (t-1) find the Segment 
		/// with the largest number of active synapses.
		/// </summary>
		/// <remarks>
        /// However only consider segments that predict activation in the number of 
        /// time steps of the active segment of this cell with the least number of 
        /// steps until activation, + 1.  For example if right now this cell is being 
        /// predicted to occur in t+2 at the earliest, then we want to find the best 
        /// segment from last time step that would predict for t+3.
        /// This routine is aggressive in finding the best match. The permanence
        /// value of synapses is allowed to be below connectedPerm.
        /// The number of active synapses is allowed to be below activationThreshold,
        /// but must be above minThreshold. The routine returns that segment.
		/// If no segments are found, then None is returned.
		/// </remarks>
        public Segment GetBestMatchingPreviousSegment()
        {
            return this.GetBestMatchingSegment(this.NumPredictionSteps + 1, true);
        }

		/// <summary>
		/// Gets the best matching Segment.
		/// </summary>
		/// <remarks>
        /// For this cell (at t-1 if previous=True else at t), find the Segment (only
        /// consider sequence segments if isSequence is True, otherwise only consider
        /// non-sequence segments) with the largest number of active synapses. 
        /// This routine is aggressive in finding the best match. The permanence 
        /// value of synapses is allowed to be below connectedPerm. 
        /// The number of active synapses is allowed to be below activationThreshold, 
        /// but must be above minThreshold. The routine returns that segment. 
		/// If no segments are found, then None is returned.
		/// </remarks>
		/// <param name="numPredictionSteps">Number of time steps in the future an activation will occur.</param>
		/// <param name="previous">If true, returns for previous time step. Defaults to false.</param>
        public Segment GetBestMatchingSegment(int numPredictionSteps, bool previous)
        {
            Segment bestSegment = null;
            int bestSynapseCount = this.Column.MinOverlapToReuseSegment;

            foreach (Segment seg in this.Segments)
            {
                if (seg.NumPredictionSteps != numPredictionSteps)
                {
                    continue;
                }

                int synCount = previous
                                   ? seg.GetPrevActiveSynapseCount()
                                   : seg.GetActiveSynapseCount();

                if (synCount >= bestSynapseCount)
                {
                    bestSynapseCount = synCount;
                    bestSegment = seg;
                }
            }

            return bestSegment;
        }
    }
}
