using System.Diagnostics;

namespace CLA
{
    /// <summary>
    /// Description of DataSource.
    /// </summary>
    public interface IDataSource
    {
        void Initialize();
        object GetNextRecord();
    }

    /// <summary>
    /// Description of Encoder.
    /// </summary>
    public interface IEncoder
    {
        int[,,] EncodeToHtm(object rawData);
        object DecodeFromHtm(float[,,] htmData);
    }

    public class Sensor : DataSpace
    {
        private int[,,] _data;

		public new int HypercolumnDiameter
        {
            get
            {
                return 1;
            }
        }

        public IDataSource DataSource;
        public IEncoder Encoder;

        /// <summary>
        /// Indicates if file reading was started or not.
        /// </summary>
		public bool Initialized;

        public Sensor(string id, int sizeX, int sizeY, int sizeZ, IDataSource dataSource, IEncoder encoder) : base(id)
        {
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            this.SizeZ = sizeZ;
            this.DataSource = dataSource;
            this.Encoder = encoder;

            // Create data array.
            this._data = new int[this.SizeX,this.SizeY,this.SizeZ];
        }

        /// <summary>
        /// Initialize data source and encoder.
        /// </summary>
        public void Initialize()
        {
            this.DataSource.Initialize();
        }

        /// <summary>
        /// Perfoms actions related to time step progression.
        /// </summary>
        public override void NextTimeStep()
        {
            // If list reading did not start, then place the cursor on the first record
            if (!this.Initialized)
            {
                this.Initialize();
                this.Initialized = true;
            }

            // Get next record in stream
            // If reading is over then start from scratch again
            object rawData = this.DataSource.GetNextRecord();

            // Initialize the vector for representing the current record
            // This involve process raw data and then sparse it to a matrix
            this._data = this.Encoder.EncodeToHtm(rawData);
        }

        public override DataSpaceType GetDataSpaceType()
        {
            return DataSpaceType.Sensor;
        }

        public override bool GetIsActive(int x, int y, int z)
        {
			if (Global.Debug)
			{
				Debug.Assert ((x >= 0) && (x < this.SizeX));
				Debug.Assert ((y >= 0) && (y < this.SizeY));
				Debug.Assert ((z >= 0) && (z < this.SizeZ));
			}

            return (this._data[x, y, z] != 0);
        }
    }
}
