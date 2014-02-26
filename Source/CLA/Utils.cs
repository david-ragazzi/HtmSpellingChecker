using System;

namespace CLA
{
    public class DataPoint
    {
        public int X, Y, Z;

        public DataPoint()
        {
            this.X = this.Y = this.Z = 0;
        }

        public DataPoint(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    public class WeightedDataPoint
    {
        public DataPoint Point;
        public float Weight, Distance;

        public WeightedDataPoint()
        {
            this.Point = new DataPoint();
            this.Weight = 0.0f;
            this.Distance = 0.0f;
        }

        public WeightedDataPoint(int x, int y, int z, float weight, float distance)
        {
            this.Point = new DataPoint(x, y, z);
            this.Weight = weight;
            this.Distance = distance;
        }
    }

    public class Area
    {
        public int MinX, MaxX, MinY, MaxY;

        public Area()
        {
            this.MinX = this.MaxX = this.MinY = this.MaxY = 0;
        }

        public Area(int minX, int minY, int maxX, int maxY)
        {
            this.MinX = minX;
            this.MaxX = maxX;
            this.MinY = minY;
            this.MaxY = maxY;
        }

        public int GetArea()
        {
            return (this.MaxX - this.MinX + 1) * (this.MaxY - this.MinY + 1);
        }
    }

    internal class GaussianRandom
    {
        private double _mean;
        private double _standardDeviation;

        /// <summary>
		/// Initializes a new instance of the class.
        /// </summary>
        /// <param name="mean">The mean of the distribution. Default is zero.</param>
        /// <param name="standardDeviation">The standard deviation of the distribution. Default is one.</param>
        public GaussianRandom(double mean = 0, double standardDeviation = 1)
        {
            this._mean = mean;
            this._standardDeviation = standardDeviation;
        }

        /// <summary>
        /// Obtains normally (Gaussian) distributed random numbers, using the Box-Muller
        /// transformation. This transformation takes two uniformly distributed deviates
        /// within the unit circle, and transforms them into two independently
        /// distributed normal deviates.
        /// </summary>
        public double Next(Random random)
        {
            double max = this._mean + this._standardDeviation;
            double min = this._mean - this._standardDeviation;
            double normalGaussian;

            do
            {
                // two random values between 0.0 and 1.0
                double u1 = random.NextDouble();
                double u2 = random.NextDouble();

                double r = Math.Sqrt(-2.0 * Math.Log(u1));
                double theta = 2.0 * Math.PI * u2;
                double normal = r * Math.Sin(theta);

                normalGaussian = this._mean + this._standardDeviation * normal;
            } while (normalGaussian < min || normalGaussian > max);

            return normalGaussian;
        }
    }
}
