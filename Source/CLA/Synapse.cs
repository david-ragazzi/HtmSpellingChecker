using System;

namespace CLA
{
    public class SynapseParams
	{
		/// <summary>
		/// Synapses with permanences above this value are connected.
		/// </summary>
        public float ConnectedPerm;

		/// <summary>
		/// Initial permanence for distal synapses.
		/// </summary>
        public float InitialPermanence;

		/// <summary>
		/// Amount permanences of synapses are decremented in learning.
		/// </summary>
        public float PermanenceDec;

		/// <summary>
		/// Amount permanences of synapses are incremented in learning.
		/// </summary>
        public float PermanenceInc;

        public SynapseParams()
        {
            this.ConnectedPerm = 0.2f;
            this.InitialPermanence = this.ConnectedPerm + 0.1f;
            this.PermanenceDec = 0.015f;
            this.PermanenceInc = 0.015f;
        }
    }

	/// <summary>
    /// A data structure representing a synapse. Contains a permanence value to
	/// indicate connectivity to a target cell.  
	/// </summary>
    public abstract class Synapse
    {
        public SynapseParams SynapseParams;

		/// <summary>
        /// Returns true if this Synapse is currently connecting its source
		/// and destination Cells.
		/// </summary>
        public bool IsConnected
        {
            get
            {
                return (this.Permanence >= this.SynapseParams.ConnectedPerm);
            }
        }

		/// <summary>
		/// A value to indicate connectivity to a target cell.  
		/// </summary>
		public float Permanence;

        public abstract bool GetIsActive();

        public virtual bool GetWasActive()
        {
            return false;
        }

        public virtual bool GetWasActiveFromLearning()
        {
            return false;
        }

        public virtual bool GetIsActiveFromLearning()
        {
            return false;
        }

        public Synapse()
        {
        }

        public void Initialize(SynapseParams synapseParams)
        {
            this.SynapseParams = synapseParams;
            this.Permanence = 0.0f;
        }

		/// <summary>
		/// Decrease the permance value of the synapse
		/// </summary>
        public void DecreasePermanence()
        {
            this.Permanence = Math.Max(0.0f, this.Permanence - this.SynapseParams.PermanenceDec);
        }

		/// <summary>
		/// Decrease the permance value of the synapse
		/// </summary>
		/// <param name="amount">Amount to decrease</param>
        public void DecreasePermanence(float amount, float min = 0.0f)
        {
            this.Permanence = Math.Max(min, this.Permanence - amount);
        }

		/// <summary>
		/// Decrease the permance value of the synapse, with no lower limit.
		/// </summary>
        public void DecreasePermanenceNoLimit()
        {
            this.Permanence = this.Permanence - this.SynapseParams.PermanenceDec;
        }

		/// <summary>
		/// Limit permanence value to >= 0. This is done after a call to DecreasePermanenceNoLimit.
		/// </summary>
        public void LimitPermanenceAfterDecrease()
        {
            if (this.Permanence < 0.0f)
            {
                this.Permanence = 0.0f;
            }
        }

		/// <summary>
		/// Increase the permanence value of the synapse.
		/// </summary>
        public void IncreasePermanence()
        {
            this.Permanence = Math.Min(1.0f, this.Permanence + this.SynapseParams.PermanenceInc);
        }

		/// <summary>
		/// Increase the permanence value of the synapse.
		/// </summary>
		/// <param name="amount">Amount to increase.</param>
        public void IncreasePermanence(float amount, float max = 1.0f)
        {
            this.Permanence = Math.Min(max, this.Permanence + amount);
        }
    }
}
