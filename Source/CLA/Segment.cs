using System;
using System.Collections.Generic;

namespace CLA
{
	/// <summary>
	/// Represents a single dendrite segment that forms synapses (connections) to other Cells.
	/// </summary>
	/// <remarks>
    /// Each Segment also maintains a boolean flag indicating 
    /// whether the Segment predicts feed-forward input on the next time step.
    /// Segments can be either proximal or distal (for spatial pooling or temporal pooling 
    /// respectively) however the class object itself does not need to know which
    /// it ultimately is as they behave identically.  Segments are considered 'active' 
	/// if enough of its existing synapses are connected and individually active.
	/// </remarks>
    public class Segment
	{
		/// <summary>
		/// Maximum number of prediction steps to track.
		/// </summary>
        public const int MaxTimeSteps = 10;

        public int ConnectedSynapsesCount, PrevConnectedSynapsesCount;

		/// <summary>
        /// Returns true if the number of connected synapses on this 
        /// Segment that are active due to active states at time t is 
		/// greater than activationThreshold.
		/// </summary>
		public bool IsActive;

		/// <summary>
        /// Returns true if the number of connected synapses on this segmentUpdateList
		/// that were active due to active states at time t-1 is greater than ActiveThreshold.
		/// </summary>
		public bool WasActive;

		/// <summary>
		/// The synapses list.
		/// </summary>
        public List<Synapse> Synapses = new List<Synapse>();

		/// <summary>
        /// The list of all active synapses as computed as of the most recently processed
		/// time step for this segment.
		/// </summary>
        public List<Synapse> ActiveSynapses = new List<Synapse>();

		/// <summary>
        /// The list of all previously active (in t-1) synapses as computed as of the most
		/// recently processed time step for this segment.
		/// </summary>
        public List<Synapse> PrevActiveSynapses = new List<Synapse>();

		/// <summary>
		/// Returns true if the Segment predicts feed-forward input on the next time step.
		/// </summary>
        public bool IsSequence
        {
            get
            {
                return (this._numPredictionSteps == 1);
            }
        }

		/// <summary>
		/// Define the number of time steps in the future an activation will occur
		/// in if this segment becomes active.
		/// </summary>
		/// <remarks>
        /// For example if the segment is intended to predict activation in the very next 
        /// time step (t+1) then this value is 1. If the value is 2 this segment is said 
        /// to predict its Cell will activate in 2 time steps (t+2) etc.  By definition if 
		/// a segment is a sequence segment it has a value of 1 for prediction steps.
		/// </remarks>
        public int NumPredictionSteps
        {
            get
            {
                return this._numPredictionSteps;
            }
            set
            {
                this._numPredictionSteps = Math.Min(Math.Max(1, value), MaxTimeSteps);
            }
        }

        private int _numPredictionSteps;

		/// <summary>
		/// A threshold number of active synapses between active and non-active Segment state.
		/// </summary>
		public float ActiveThreshold;

		public int CreationTime;

		/// <summary>
        /// Return a count of how many connected synapses on this segment are active
		/// in the current time step.
		/// </summary>
		public int ActiveConnectedSynapsesCount;

		/// <summary>
        /// Return a count of how many connected synapses on this segment 
		/// were active in the previous time step.
		/// </summary>
		public int PrevActiveConnectedSynapsesCount;

		/// <summary>
        /// Return a count of how many synapses on this segment are from active learning cells
		/// in the current time step.
		/// </summary>
		public int ActiveLearningSynapsesCount;

		/// <summary>
        /// Return a count of how many synapses on this segment were from active learning cells
		/// in the previous time step.
		/// </summary>
		public int PrevActiveLearningSynapsesCount;

		/// <summary>
		/// Return a count of how many synapses on this segment are connected above InitialPerm, but are not active.
		/// </summary>
		public int InactiveWellConnectedSynapsesCount;

        public Segment()
        {
        }

		/// <summary>
        /// Returns true if the number of connected synapses on this 
        /// Segment that were active due to learning states at time t-1 is 
		/// greater than activationThreshold.
		/// </summary>
        public bool GetWasActiveFromLearning()
        {
            int numberSynapsesWasActive = 0;

            foreach (Synapse syn in this.Synapses)
            {
                numberSynapsesWasActive += syn.GetWasActiveFromLearning()
                                               ? 1
                                               : 0;
            }
            return numberSynapsesWasActive >= this.ActiveThreshold;
        }

		/// <summary>
		/// Initializes a new instance of the Segment class.
		/// </summary>
		/// <param name="activeThreshold">A threshold number of active synapses between 
		/// active and non-active Segment state.</param>
        public void Initialize(int creationTime, float activeThreshold)
        {
            this._numPredictionSteps = -1;
            this.ActiveThreshold = activeThreshold;
            this.ConnectedSynapsesCount = 0;
            this.ActiveConnectedSynapsesCount = 0;
            this.PrevActiveConnectedSynapsesCount = 0;
            this.ActiveLearningSynapsesCount = 0;
            this.PrevActiveLearningSynapsesCount = 0;
            this.InactiveWellConnectedSynapsesCount = 0;
            this.IsActive = false;
            this.WasActive = false;
            this.CreationTime = creationTime;
        }

		/// <summary>
		/// Advance this segment to the next time step.
		/// </summary>
		/// <remarks>
        /// The current state of this segment (active, number of synapes) will be set as 
        /// the previous state and the current state will be reset to no cell activity by 
		/// default until it can be determined.
		/// </remarks>
        public void NextTimeStep()
        {
            this.WasActive = this.IsActive;
            this.IsActive = false;
            this.PrevConnectedSynapsesCount = this.ConnectedSynapsesCount;
            this.PrevActiveConnectedSynapsesCount = this.ActiveConnectedSynapsesCount;
            this.ActiveConnectedSynapsesCount = 0;
            this.PrevActiveLearningSynapsesCount = this.ActiveLearningSynapsesCount;
            this.ActiveLearningSynapsesCount = 0;
            this.PrevActiveSynapses = new List<Synapse>(this.ActiveSynapses);
            this.ActiveSynapses.Clear();
            this.InactiveWellConnectedSynapsesCount = 0;
        }

		/// <summary>
		/// Process this segment for the current time step.
		/// </summary>
		/// <remarks>
        /// Processing will determine the set of active synapses on this segment for this time 
        /// step.  It will also keep count of how many of those active synapses are also connected.
        /// From there we will determine if this segment is active if enough active connected
        /// synapses are present.  This information is then cached for the remainder of the
        /// Region's processing for the time step.  When a new time step occurs, the
        /// Region will call nextTimeStep() on all cells/segments to cache the 
		/// information as which synapses were previously active.
		/// </remarks>
        public void ProcessSegment()
        {
            this.ConnectedSynapsesCount = 0;
            this.ActiveConnectedSynapsesCount = 0;
            this.ActiveLearningSynapsesCount = 0;
            this.InactiveWellConnectedSynapsesCount = 0;

            this.ActiveSynapses.Clear();

            foreach (Synapse syn in this.Synapses)
            {
                if (syn.GetIsActive())
                {
                    this.ActiveSynapses.Add(syn);

                    if (syn.IsConnected)
                    {
                        this.ActiveConnectedSynapsesCount++;
                    }

                    if (syn.GetIsActiveFromLearning())
                    {
                        this.ActiveLearningSynapsesCount++;
                    }
                }
                else
                {
                    if (syn.Permanence > syn.SynapseParams.InitialPermanence)
                    {
                        this.InactiveWellConnectedSynapsesCount++;
                    }
                }

                if (syn.IsConnected)
                {
                    this.ConnectedSynapsesCount++;
                }
            }

            this.IsActive = (this.ActiveConnectedSynapsesCount >= this.ActiveThreshold);
        }

		/// <summary>
        /// Create a new proximal synapse for this segment attached to the specified 
		/// input cell.
		/// </summary>
		/// <param name="inputSource">The input source of the synapse to create.</param>
		/// <param name="initPerm">The initial permanence of the synapse.</param>
		/// <returns>Newly created synapse.</returns>
        public ProximalSynapse CreateProximalSynapse(SynapseParams synapseParams, DataSpace inputSource, ref DataPoint inputPoint, float permanence, float distanceToInput)
        {
            var newSyn = new ProximalSynapse();
            newSyn.Initialize(synapseParams, inputSource, ref inputPoint, permanence, distanceToInput);
            this.Synapses.Add(newSyn);
            return newSyn;
        }

		/// <summary>
		/// Create a new synapse for this segment attached to the specified input source.
		/// </summary>
		/// <param name="inputSource">The input source of the synapse to create.</param>
		/// <param name="initPerm">The initial permanence of the synapse.</param>
		/// <returns>Newly created synapse.</returns>
        public DistalSynapse CreateDistalSynapse(SynapseParams synapseParams, Cell inputSource, float initPerm)
        {
            var newSyn = new DistalSynapse();
            newSyn.Initialize(synapseParams, inputSource, initPerm);
            this.Synapses.Add(newSyn);
            return newSyn;
        }

		/// <summary>
        /// Create numSynapses new synapses attached to the specified
		/// set of learning cells.
		/// </summary>
		/// <param name="synapseCells">Set of available learning cells to form synapses to.</param>
		/// <param name="added">Set will be populated with synapses that were successfully added.</param>
        public void CreateSynapsesToLearningCells(ref List<Cell> synapseCells, SynapseParams synapseParams)
        {
            // Assume that cells were previously checked to prevent adding
            // synapses to the same cell more than once per segment.
            foreach (Cell cell in synapseCells)
            {
                this.CreateDistalSynapse(synapseParams, cell, synapseParams.InitialPermanence);
            }
        }

		/// <summary>
        /// Return a count of how many synapses on this segment (whether connected or not) 
		/// are active in the current time step.
		/// </summary>
        public int GetActiveSynapseCount()
        {
            return this.ActiveSynapses.Count;
        }

		/// <summary>
        /// Return a count of how many synapses on this segment (whether connected or not) 
		/// were active in the previous time step.
		/// </summary>
        public int GetPrevActiveSynapseCount()
        {
            return this.PrevActiveSynapses.Count;
        }

		/// <summary>
        /// Update all permanence values of each synapse based on current activity.
		/// If a synapse is active, increase its permanence, else decrease it.
		/// </summary>
        public void AdaptPermanences()
        {
            foreach (Synapse syn in this.Synapses)
            {
                if (syn.GetIsActive())
                {
                    syn.IncreasePermanence();
                }
                else
                {
                    syn.DecreasePermanence();
                }
            }
        }

		/// <summary>
        /// Update (increase or decrease based on whether the synapse is active)
		/// all permanence values of each of the synapses in the specified set.
		/// </summary>
        public void UpdatePermanences(ref List<Synapse> activeSynapses)
        {
            // Decrease all synapses first...
            foreach (Synapse syn in this.Synapses)
            {
                syn.DecreasePermanenceNoLimit();
            }

            // Then for each active synapse, undo its decrement and add an increment.
            foreach (Synapse syn in activeSynapses)
            {
                syn.IncreasePermanence(syn.SynapseParams.PermanenceDec + syn.SynapseParams.PermanenceInc);
            }

            // No make sure that all synapse permanence values are >= 0, since the decrement was done without enforcing a limit 
            // (so as to avoid incorrect amount of increment).
            foreach (Synapse syn in this.Synapses)
            {
                syn.LimitPermanenceAfterDecrease();
            }
        }

		/// <summary>
        /// Decrease the permanences of each of the synapses in the set of
		/// active synapses that happen to be on this segment.
		/// </summary>
        public void DecreasePermanences(ref List<Synapse> activeSynapses)
        {
            // Decrease the permanence of each synapse on this segment.
            foreach (Synapse syn in activeSynapses)
            {
                syn.DecreasePermanence();
            }
        }
    }
}
