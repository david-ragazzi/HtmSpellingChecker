using System.Diagnostics;

namespace CLA
{
	/// <summary>
    /// A data structure representing a distal synapse. Contains a permanence value and the 
	/// source input to a lower input cell.  
	/// </summary>
    public class DistalSynapse : Synapse
	{
		/// <summary>
		/// A single input from a neighbour Cell.
		/// </summary>
		public Cell InputSource;

        public DistalSynapse()
        {
        }

		/// <summary>
		/// Returns true if this DistalSynapse is active due to the current input.
		/// </summary>
        public override bool GetIsActive()
        {
			if (Global.Debug)
			{
				Debug.Assert (this.InputSource != null);
			}
            return this.InputSource.IsActive;
        }

		/// <summary>
		/// Returns true if this DistalSynapse was active due to the previous input at t-1. 
		/// </summary>
        public override bool GetWasActive()
        {
			if (Global.Debug)
			{
				Debug.Assert (this.InputSource != null);
			}
            return this.InputSource.WasActive;
        }

		/// <summary>
        /// Returns true if this DistalSynapse was active due to the input
		/// previously being in a learning state. 
		/// </summary>
        public override bool GetWasActiveFromLearning()
        {
			if (Global.Debug)
			{
				Debug.Assert (this.InputSource != null);
			}
            return (this.InputSource.WasActive) && (this.InputSource.WasLearning);
        }

		/// <summary>
        /// Returns true if this DistalSynapse is active due to the input
		/// currently being in a learning state. 
		/// </summary>
        public override bool GetIsActiveFromLearning()
        {
            Debug.Assert(this.InputSource != null);
            return (this.InputSource.IsActive) && (this.InputSource.IsLearning);
        }

		/// <summary>
        /// Initializes a new instance of the DistalSynapse class and 
		/// sets its input source and initial permanance values.
		/// </summary>
		/// <param name="inputSrc">An object providing source of the input to this synapse 
		/// (a Column's Cell).</param>
		/// <param name="permanence">Initial permanence value.</param>
        public void Initialize(SynapseParams synapseParams, Cell inputSrc, float permanence)
        {
            base.Initialize(synapseParams);

            this.InputSource = inputSrc;
            this.Permanence = permanence;
        }

		/// <summary>
		/// This version of Initialize() is used when loading data from disk.
		/// </summary>
        public new void Initialize(SynapseParams synapseParams)
        {
            base.Initialize(synapseParams);

            this.InputSource = null;
            this.Permanence = 0.0f;
        }
    }
}
