namespace CLA
{
	/// <summary>
	/// Represents a synapse that receives feed-forward input from an input cell.
	/// </summary>
    public class ProximalSynapse : Synapse
	{
		/// <summary>
		/// The DataSpace for this synapse's input.
		/// </summary>
        public DataSpace InputSource;

		/// <summary>
		/// A single input value point from an input DataSource.
		/// </summary>
        public DataPoint InputPoint;

		/// <summary>
		/// Distance, in this synapse's Region's space, to its input DataPoint.
		/// </summary>
        public float DistanceToInput;

        public ProximalSynapse()
        {
        }

		/// <summary>
		/// Returns true if this ProximalSynapse is active due to the current input.
		/// </summary>
        public override bool GetIsActive()
        {
            return this.InputSource.GetIsActive(this.InputPoint.X, this.InputPoint.Y, this.InputPoint.Z);
        }

		/// <summary>
        /// Initializes a new instance of the ProximalSynapse class and 
		/// sets its input source and initial permanance values.
		/// </summary>
		/// <param name="inputSource">A DataSource (external data source, or another Region) providing source of the input to this synapse.</param>
		/// <param name="inputPoint">Coordinates and value index of this synapse's input within the inputSource.</param>
		/// <param name="permanence">Initial permanence value.</param>
		/// <param name="distanceToInput">In the Region's hypercolumn coordinates; used by Region.AverageReceptiveFieldSize().</param>
        public void Initialize(SynapseParams synapseParams, DataSpace inputSource, ref DataPoint inputPoint, float permanence, float distanceToInput)
        {
            base.Initialize(synapseParams);

            this.InputSource = inputSource;
            this.InputPoint = inputPoint;
            this.DistanceToInput = distanceToInput;

            this.Permanence = permanence;
        }

		/// <summary>
		/// This version of Initialize() is used when loading data.
		/// </summary>
        public new void Initialize(SynapseParams synapseParams)
        {
            base.Initialize(synapseParams);

            this.InputSource = null;
            this.InputPoint = new DataPoint();
            this.DistanceToInput = 0.0f;

            this.Permanence = 0.0f;
        }
    }
}
