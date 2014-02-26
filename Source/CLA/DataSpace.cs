namespace CLA
{
    public enum DataSpaceType
    {
        Sensor = 0,
        Region = 1,
        Classifier = 2
    }

    public abstract class DataSpace
    {
		public string Id;
		public int Index;

		public int SizeX;
		public int SizeY;
		public int SizeZ;

		/// <summary>
		/// The diameter of a hypercolumn in this data space. Defaults to 1.
		/// </summary>
		public int HypercolumnDiameter = 1;

        public DataSpace(string id)
        {
            this.Id = id;
            this.Index = -1;
        }

        public abstract void NextTimeStep();

        public abstract DataSpaceType GetDataSpaceType();

        public abstract bool GetIsActive(int x, int y, int z);
    }
}
