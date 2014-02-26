using System;
using System.Diagnostics;
using System.Drawing;

namespace CLA
{
	/// <summary>
    /// Represents a single column of cells within an HTM Region.
	/// </summary>
    public class Column
	{
		/// <summary>
		/// Exponential Moving Average alpha value.
		/// </summary>
        private const float _emaAlpha = 0.005f;
        private const float _fastEmaAlpha = 0.008f;

		/// <summary>
		/// A Column's MinBoost and MaxBoost are up to this far from 1.0 and the given MaxBoost, respectively. 
		/// This is to avoid ties in overlap values between columns with no boost, or with full boost.
		/// </summary>
        private const float _boostVariance = 0.01f;

        private const float _increaseBoostThreshold = 0.01f;
        private const float _decreaseBoostThreshold = 0.65f;

        private int _sumInputVolume;

		/// <summary>
        /// This variable determines the desired amount of columns to be activated within a
		/// given spatial pooling inhibition radius.
		/// For example if the InhibitionRadius is
        /// 3 it means we have 7x7 grid of hypercolumns under local consideration.  During inhibition
        /// we need to decide which columns in this local area are to be activated.  If 
        /// DesiredLocalActivity is 5 then we take only the 5 most strongly activated columns
		/// within the 7x7 local grid of hypercolumns.
		/// </summary>
        public int DesiredLocalActivity;

        public Point Position;

        public Region Region;

        public float ActiveDutyCycle;

        public float FastActiveDutyCycle;

        public float OverlapDutyCycle;

        public float MaxDutyCycle;

		/// <summary>
        /// The minimum number of inputs that must be active for a column to be 
        /// considered during the inhibition step. Value established by input parameters
		/// and receptive-field size.
		/// </summary>
        public double MinOverlap;

		/// <summary>
        // This parameter determines whether a segment will be considered a match to activity. It may be considered a match if at least
        // this number of the segment's synapses match the actvity. The segment will then be re-used to represent that activity, with new syanpses
        // added to fill out the pattern. The lower this number, the more patterns will be added to a single segment, which can be very bad because
        // the same cell is thus used to represent an input in diffrent contexts, and also if the ratio of PermanenceInc to PermanenceDec
		// is such that multiple patterns cannot be supported on one synapse, so all but 1 will generally remain disconnected, so predictions are never made.
		/// </summary>
        public int MinOverlapToReuseSegment;

		/// <summary>
		/// A proximal dendrite segment forms synapses with feed-forward inputs.
		/// </summary>
        public Segment ProximalSegment;

        public Cell[] Cells;

		/// <summary>
		/// Toggle whether or not this Column is currently active.
		/// </summary>
        public bool IsActive;

		/// <summary>
		/// Boost-Factor.
		/// </summary>
		/// <remarks>
        /// The boost value is a scalar >= 1. 
        /// While activeDutyCyle(c) remains above minDutyCycle(c), the boost value remains at 1. 
        /// The boost increases linearly once the column's activeDutyCycle starts falling
		/// below its minDutyCycle.
		/// </remarks>
        public float Boost;

        public float MinBoost;
        public float MaxBoost;

        public int PrevBoostTime;

		/// <summary>
        /// The spatial pooler overlap with a particular input pattern. Just connected proximal synapses 
        /// count to overlap.
        /// Overlap is stored as a float because it represents not just the proximal segment's number of
		/// active connected synapses, but then multiplies that number by the boost factor.
		/// </summary>
        public float Overlap;

		/// <summary>
        /// Results from computeColumnInhibition. 
		/// Only possible if Overlap > MinOverlap but Overlap LT MinLocalActivity.
		/// </summary>
        public bool IsInhibited;

		/// <summary>
		/// Generic randomizer
		/// </summary>
        private static Random _random = new Random();
        private static Random _generator = new Random();

		/// <summary>
        /// Initializes a new Column for the given parent Region at source 
		/// row/column position srcPos and column grid position pos.
		/// </summary>
		/// <param name="region">The parent Region this Column belongs to.</param>
		/// <param name="srcPos">A Point (srcX,srcY) of this Column's 'center' position in 
		/// terms of the proximal-synapse input space.</param>
		/// <param name="pos">A Point(x,y) of this Column's position within the Region's 
		/// column grid.</param>
        public Column(Region region, Point pos, int minOverlapToReuseSegment)
        {
            this.Region = region;

            this.IsActive = false;
            this.IsInhibited = false;
            this.ActiveDutyCycle = 1;
            this.FastActiveDutyCycle = 1;
            this.OverlapDutyCycle = 1.0f;
            this.MaxDutyCycle = 0.0f;
            this.Overlap = 0;
            this.PrevBoostTime = 0;
            this.DesiredLocalActivity = 0;
            this.MinOverlapToReuseSegment = minOverlapToReuseSegment;

            // Determine initial random low Boost value, just to break ties between columns with the same amount of overlap.
            // The initial Boost value is set to be the same as this Column's MinBoost value.
            this.Boost = this.MinBoost = 1.0f + (float)_random.NextDouble() * _boostVariance;

            // Determine this Column's MaxBoost value, with random variation to avoid ties between fully boosted Columns.
            this.MaxBoost = (this.Region.MaxBoost == -1)
                                ? -1
                                : this.Region.MaxBoost - (float)_random.NextDouble() * _boostVariance;

            // Create each of this Column's Cells.
            this.Cells = new Cell[this.Region.CellsPerCol];
            for (int i = 0; i < this.Region.CellsPerCol; i++)
            {
                var newCell = new Cell();
                newCell.Initialize(this, i);
                this.Cells[i] = newCell;
            }

            // The list of potential proximal synapses and their permanence values.
            this.ProximalSegment = new Segment();
            this.ProximalSegment.Initialize(0, this.Region.SegActiveThreshold);

            // Record Position and determine HypercolumnPosition.
            this.Position = pos;
        }

		/// <summary>
        ///  Create a new ProximalSynapse corresponding to the random sample's X, Y, and Z values.
		/// </summary>
		/// <remarks>
        /// inputRadii
        /// One value per input DataSpace.
        /// Limit a section of the total input to more effectively learn lines or corners in a
        /// small section without being 'distracted' by learning larger patterns in the overall
        /// input space (which hopefully higher hierarchical Regions would handle more 
        /// successfully). Passing in -1 for input radius will mean no restriction.
        ///   
        /// Prior to receiving any inputs, the region is initialized by computing a list of 
        /// initial potential synapses for each column. This consists of a random set of inputs
        /// selected from the input space. Each input is represented by a synapse and assigned
        /// a random permanence value. The random permanence values are chosen with two 
        /// criteria. First, the values are chosen to be in a small range around connectedPerm
        /// (the minimum permanence value at which a synapse is considered "connected"). This 
        /// enables potential synapses to become connected (or disconnected) after a small 
        /// number of training 
        /// iterations. Second, each column has a natural center over the input region, and 
        /// the permanence values have a bias towards this center (they have higher values
        /// near the center).
        /// 
        /// The concept of Input Radius is an additional parameter to control how 
        /// far away synapse connections can be made instead of allowing connections anywhere.  
        /// The reason for this is that in the case of video images I wanted to experiment 
        /// with forcing each Column to only learn on a small section of the total input to 
		/// more effectively learn lines or corners in a small section
		/// </remarks>
        public void CreateProximalSegments()
        {
            // Initialize values.
            this._sumInputVolume = 0;
            this.MinOverlap = 0;

            // Determine this Column's hypercolumn coordinates.
            int destHcolX = this.Position.X / this.Region.HypercolumnDiameter;
            int destHcolY = this.Position.Y / this.Region.HypercolumnDiameter;

            // Determine the center of this Column's hypercolumn, in this Column's Region's space.
            float inputCenterX = (destHcolX + 0.5f) / ((float)this.Region.SizeX / this.Region.HypercolumnDiameter);
            float inputCenterY = (destHcolY + 0.5f) / ((float)this.Region.SizeY / this.Region.HypercolumnDiameter);

            // Iterate through each input DataSpace, creating synapses for each one, in each column's proximal segment.
            foreach (Input input in this.Region.InputList)
            {
                // Get a pointer to the current input DataSpace.
                DataSpace inputDataSpace = input.DataSpace;

                // Get the input radius corresponding to the current input space.
                int inputRadius = input.Radius;

                // Determine the center of the receptive field, in the input space's hypercolumn coordinates.
                float srcHcolX = Math.Min((int)(inputCenterX * ((float)inputDataSpace.SizeX / inputDataSpace.HypercolumnDiameter)), (inputDataSpace.SizeX / inputDataSpace.HypercolumnDiameter - 1));
                float srcHcolY = Math.Min((int)(inputCenterY * ((float)inputDataSpace.SizeY / inputDataSpace.HypercolumnDiameter)), (inputDataSpace.SizeY / inputDataSpace.HypercolumnDiameter - 1));

                Area inputAreaHcols = inputRadius == -1
                                          ? new Area(0, 0, inputDataSpace.SizeX / inputDataSpace.HypercolumnDiameter - 1, inputDataSpace.SizeY / inputDataSpace.HypercolumnDiameter - 1)
                                          : new Area((int)Math.Max(0, srcHcolX - inputRadius), (int)Math.Max(0, srcHcolY - inputRadius), (int)Math.Min(inputDataSpace.SizeX / inputDataSpace.HypercolumnDiameter - 1, srcHcolX + inputRadius), (int)Math.Min(inputDataSpace.SizeY / inputDataSpace.HypercolumnDiameter - 1, srcHcolY + inputRadius));

                // Determine the input area in columns.
                var inputAreaCols = new Area(inputAreaHcols.MinX * inputDataSpace.HypercolumnDiameter, inputAreaHcols.MinY * inputDataSpace.HypercolumnDiameter, (inputAreaHcols.MaxX + 1) * inputDataSpace.HypercolumnDiameter - 1, (inputAreaHcols.MaxY + 1) * inputDataSpace.HypercolumnDiameter - 1);

                // Compute volume (in input values) of current input area.
                int inputVolume = inputAreaCols.GetArea() * inputDataSpace.SizeZ;

                // Add the current input volume to the _sumInputVolume (used by ComputeOverlap()).
                this._sumInputVolume += inputVolume;

                // Proximal synapses per Column (input segment) for the current input.
                var synapsesPerSegment = (int)(inputVolume * this.Region.PctInputPerColumn + 0.5f);

                // The minimum number of inputs that must be active for a column to be 
                // considered during the inhibition step. Sum for all input DataSpaces.
                this.MinOverlap += Math.Ceiling(synapsesPerSegment * this.Region.PctMinOverlap);

                var inputSpaceArray = new WeightedDataPoint[inputVolume];
                float sumWeight = 0.0f;

                // Fill the InputSpaceArray with coords of each data point in the input space, 
                // and a weight value inversely proportial to distance from the center of the field.
                int pos = 0;
                for (int hy = inputAreaHcols.MinY; hy <= inputAreaHcols.MaxY; hy++)
                {
                    for (int hx = inputAreaHcols.MinX; hx <= inputAreaHcols.MaxX; hx++)
                    {
                        // Determine the distance of the current input hypercolumn from the center of the field, in the source input space's coordinates.
                        float dX = srcHcolX - hx;
                        float dY = srcHcolY - hy;
                        var distanceToInputInSrcSpace = (float)Math.Sqrt(dX * dX + dY * dY);

                        // Determine the distance of the current input hypercolumn from the center of the field, in the destination region's coordinates.
                        dX *= (float)this.Region.SizeX / this.Region.HypercolumnDiameter / ((float)inputDataSpace.SizeX / inputDataSpace.HypercolumnDiameter);
                        dY *= (float)this.Region.SizeY / this.Region.HypercolumnDiameter / ((float)inputDataSpace.SizeY / inputDataSpace.HypercolumnDiameter);
                        var distanceToInputInDstSpace = (float)Math.Sqrt(dX * dX + dY * dY);

                        //// Determine this input hypercolumn's weight based on its distance from the input hypercolumn at the center of the receptive field.
                        //// There will be zero probability of synapses to inputs beyond a distance of inputRadius.
                        ////weight = pow(1.0 - Min(1.0, distanceToInput_InputSpace / (double)(inputRadius)), 0.5);

                        // Each hypercolumn with distance from center of receptive field less than inputRadius +1 has weight.
                        float weight = (distanceToInputInSrcSpace < (inputRadius + 1))
                                           ? 1
                                           : 0;

                        for (int y = 0; y < inputDataSpace.HypercolumnDiameter; y++)
                        {
                            for (int x = 0; x < inputDataSpace.HypercolumnDiameter; x++)
                            {
                                for (int z = 0; z < inputDataSpace.SizeZ; z++)
                                {
                                    inputSpaceArray[pos] = new WeightedDataPoint();
                                    inputSpaceArray[pos].Point.X = (inputDataSpace.HypercolumnDiameter * hx) + x;
                                    inputSpaceArray[pos].Point.Y = (inputDataSpace.HypercolumnDiameter * hy) + y;
                                    inputSpaceArray[pos].Point.Z = z;
                                    inputSpaceArray[pos].Distance = distanceToInputInDstSpace;
                                    inputSpaceArray[pos].Weight = weight;
                                    sumWeight += weight;
                                    pos++;
                                }
                            }
                        }
                    }
                }
				if (Global.Debug)
				{
					Debug.Assert (pos == inputVolume);
				}

                // Initialize gaussian distribution random number generator. 
                var gausianNormalDistribution = new GaussianRandom(this.Region.ProximalSynapseParams.ConnectedPerm, this.Region.ProximalSynapseParams.PermanenceInc);

                // Generate synapsesPerSegment samples, moving thier WeightedDataPoint records to the beginning of the InputSpaceArray.
                int numSamples = 0;
                while (numSamples < synapsesPerSegment)
                {
                    // Determine a sample within the range of the sum weight of all points that have not yet been selected as samples.
                    float curSample = (float)_random.NextDouble() * sumWeight;

                    // Iterate through all remaining points that have not yet been selected as samples...
                    float curSampleSumWeight = 0.0f;
                    int curSamplePos;
                    for (curSamplePos = numSamples; curSamplePos < inputVolume; curSamplePos++)
                    {
                        curSampleSumWeight += inputSpaceArray[curSamplePos].Weight;

                        // If the random sample targets the current point, exit the loop.
                        if (curSampleSumWeight >= curSample)
                        {
                            break;
                        }
                    }

                    // Subtract the weight of the sampled point from the sumWeight of all points that haven't yet been selected as samples.
                    sumWeight -= inputSpaceArray[curSamplePos].Weight;

                    // Determine the permanence value for the new Syanpse.
                    var permanence = (float)gausianNormalDistribution.Next(_generator);

                    // Create the proximal synapse for the current sample.
                    this.ProximalSegment.CreateProximalSynapse(this.Region.ProximalSynapseParams, inputDataSpace, ref inputSpaceArray[curSamplePos].Point, permanence, inputSpaceArray[curSamplePos].Distance);

                    if (curSamplePos != numSamples)
                    {
                        // Swap the Point values at the curSamplePos position with that at the numSamples position.
                        // This way, all samples are moved to the beginning of the InputSpaceArray, below numSamples.
                        WeightedDataPoint tempPoint = inputSpaceArray[numSamples];
                        inputSpaceArray[numSamples] = inputSpaceArray[curSamplePos];
                        inputSpaceArray[curSamplePos] = tempPoint;
                    }

                    // Increment numSamples.
                    numSamples++;
                }
            }

            // Overlap must be at least 1.
            if (this.MinOverlap <= 0)
            {
                this.MinOverlap = 1;
            }
        }

		/// <summary>
        /// For this column, return the cell with the best matching Segment 
        /// (at time t-1 if prevous=True else at time t). Only consider segments that are 
        /// predicting cell activation to occur in exactly numPredictionSteps many 
        /// time steps from now. If no cell has a matching segment, then return the 
		/// cell with the fewest number of segments.
		/// </summary>
		/// <param name="numPredictionSteps">Only consider segments that are predicting
        /// cell activation to occur in exactly this many time steps from now.
        /// if true only consider active segments from t-1 else 
		/// consider active segments right now.</param>
		/// <returns>Tuple containing the best cell and its best Segment. (may be None).</returns>
        public void GetBestMatchingCell(int numPredictionSteps, bool previous, out Cell bestCell, out Segment bestSegment)
        {
            // Initialize return values
            bestCell = null;
            bestSegment = null;

            int bestCount = 0;
            Cell cell;

            for (int cellIndex = 0; cellIndex < this.Region.CellsPerCol; cellIndex++)
            {
                cell = this.Cells[cellIndex];

                Segment seg = cell.GetBestMatchingSegment(numPredictionSteps, previous);

                if (seg != null)
                {
                    int synCount = previous
                                       ? seg.GetPrevActiveSynapseCount()
                                       : seg.GetActiveSynapseCount();

                    if (synCount > bestCount)
                    {
                        bestCell = cell;
                        bestSegment = seg;
                        bestCount = synCount;
                    }
                }
            }

            int fewestNumSegments = int.MaxValue;
            int sameNumSegmentsCount = 0;

            // If there are no active sequences, return the cell with the fewest number of segments.
            // If there are multiple cells with the same fewest number of segments, choose one at random.
            // This is necessary as described here: http://sourceforge.net/p/openhtm/discussion/htm/thread/ccedad1f/
            if (bestCell == null)
            {
                for (int cellIndex = 0; cellIndex < this.Region.CellsPerCol; cellIndex++)
                {
                    cell = this.Cells[cellIndex];
                    int numSegments = cell.Segments.Count;

                    // Keep count of how many cells have this same (fewest) number of segments.
                    if (numSegments < fewestNumSegments)
                    {
                        sameNumSegmentsCount = 1;
                    }
                    else if (numSegments == fewestNumSegments)
                    {
                        sameNumSegmentsCount++;
                    }

                    // If the current cell has the fewest number of segments seen yet, record that it is the new bestCell.
                    // If the current cell has the same number of segments as previously found cell(s) with the fewest 
                    // number of segments, then decide randomly whether to keep the previous pick or select the current
                    // cell as the bestCell instead. The probability of selecting this cell is base don the number of cells
                    // found so far with this fewest number of segments; for the 2nd cell there is 1/2 chance, for the
                    // 3rd cell there is 1/3 chance, etc. The result is correctly that if there are e.g. 10 cells with the 
                    // same fewest number of segments, each one will have a 1/10 chance of being selected.
                    if ((numSegments < fewestNumSegments) || ((numSegments == fewestNumSegments) && ((_random.Next() % sameNumSegmentsCount) == 0)))
                    {
                        fewestNumSegments = numSegments;
                        bestCell = cell;
                    }
                }

                //leave bestSegment null to indicate a new segment is to be added.
            }
        }

		/// <summary>
		/// The spatial pooler overlap of this column with a particular input pattern.
		/// </summary>
		/// <remarks>
        /// The overlap for each column is simply the number of connected synapses with active 
        /// inputs, multiplied by its boost. If this value is below MinOverlap, we set the 
        /// overlap score to zero.
        /// Attention: refactored regarding MinOverlap from column: overlap is now computed as 
        /// the former overlap per area as this will make areas with inequal size comparable
		/// (Columns near the edges of the Region will have smaller input areas).
		/// </remarks>
        public void ComputeOverlap()
        {
            // Calculate number of active synapses on the proximal segment
            this.ProximalSegment.ProcessSegment();

            // Find "overlap", that is the current number of active and connected synapses
            float overlap = this.ProximalSegment.ActiveConnectedSynapsesCount;

            if (overlap < this.MinOverlap)
            {
                overlap = 0;
            }
            else
            {
                // Set Overlap to be the number of active connected synapses, multiplid by the Boost factor, and mutiplied by the ratio of the 
                // active connected synapses count over the active connected synapses count plus the inactive well-connected (>=InitialPerm) synpses count.
                // This last term penalizes a match if it includes many strongly connected synapses that are not active,
                // so that patterns with greater numbers of connected syanpses do not gain an advantage in representing all possible subpatterns.
                // It only cares about strongly connected synapses, because weakly connected synapses can be the result of little or no learning, and we don't
                // want to penalize matches that haven't had a chance to be refined by learning yet.
                overlap = overlap * ((float)this.ProximalSegment.ActiveConnectedSynapsesCount / (this.ProximalSegment.ActiveConnectedSynapsesCount + this.ProximalSegment.InactiveWellConnectedSynapsesCount)) * this.Boost;
            }

            // Record the determined number as this Column's Overlap.
            this.Overlap = overlap;
        }

		/// <summary>
		// Return the Area of Columns that are withi the given hypercolumn radius of this Column's hypercolumn.
		/// </summary>
        public Area DetermineColumnsWithinHypercolumnRadius(int radius)
        {
            return new Area(Math.Max(0, ((this.Position.X / this.Region.HypercolumnDiameter) - radius) * this.Region.HypercolumnDiameter), Math.Max(0, ((this.Position.Y / this.Region.HypercolumnDiameter) - radius) * this.Region.HypercolumnDiameter), Math.Min(this.Region.SizeX - 1, ((this.Position.X / this.Region.HypercolumnDiameter) + radius + 1) * this.Region.HypercolumnDiameter - 1), Math.Min(this.Region.SizeY - 1, ((this.Position.Y / this.Region.HypercolumnDiameter) + radius + 1) * this.Region.HypercolumnDiameter - 1));
        }

		/// <summary>
		// Determine this Column's DesiredLocalActivity, being PctLocalActivity of the number of columns wthin its Region's InhibitionRadius of hypercolumns.
		/// </summary>
        public void DetermineDesiredLocalActivity()
        {
            // Determine the extent, in columns, of the area within the InhibitionRadius of hypercolumns.
            Area inhibitionArea = this.DetermineColumnsWithinHypercolumnRadius((int)(this.Region.InhibitionRadius + 0.5f));

            // The DesiredLocalActivity is the PctLocalActvity multiplied by the area over which inhibition will take place.
            this.DesiredLocalActivity = (int)(inhibitionArea.GetArea() * this.Region.PctLocalActivity + 0.5f);
        }

		/// <summary>
        /// Compute whether or not this Column will be active after the effects of local
        /// inhibition are applied.  A Column must have overlap greater than 0 and have its
        /// overlap value be within the k'th largest of its neighbors 
		/// (where k = desiredLocalActivity).
		/// </summary>
        public void ComputeColumnInhibition()
        {
            this.IsActive = false;
            this.IsInhibited = false;

            if (this.Overlap > 0)
            {
                if (this.Region.IsWithinKthScore(this, this.DesiredLocalActivity))
                {
                    this.IsActive = true;
                }
                else
                {
                    this.IsInhibited = true;
                }
            }
        }

		/// <summary>
        /// Update the permanence value of every synapse in this column based on whether active.
        /// This is the main learning rule (for the column's proximal dentrite). 
        /// For winning columns, if a synapse is active, its permanence value is incremented, 
		/// otherwise it is decremented. Permanence values are constrained to be between 0 and 1.
		/// </summary>
        public void AdaptPermanences()
        {
            this.ProximalSegment.AdaptPermanences();
        }

		/// <summary>
		/// Increase the permanence value of every unconnected proximal synapse in this column by the amount given.
		/// </summary>
        public void BoostPermanences(float amount)
        {
            foreach (Synapse syn in this.ProximalSegment.Synapses)
            {
                if (syn.Permanence < this.Region.ProximalSynapseParams.ConnectedPerm)
                {
                    syn.IncreasePermanence(amount, this.Region.ProximalSynapseParams.ConnectedPerm);
                }
                else if (syn.Permanence > this.Region.ProximalSynapseParams.ConnectedPerm)
                {
                    syn.DecreasePermanence(amount, this.Region.ProximalSynapseParams.ConnectedPerm);
                }
            }
        }

		/// <summary>
		/// Update running averages of activity and overlap.
		/// </summary>
        public void UpdateDutyCycles()
        {
            this.MaxDutyCycle = this.DetermineMaxDutyCycle();

            this.UpdateActiveDutyCycle();

            this.UpdateOverlapDutyCycle();
        }

		/// <summary>
        /// There are two separate boosting mechanisms in place to help a column learn connections. 
        /// If a column does not win often enough (as measured by activeDutyCycle), its overall 
        /// boost value is increased (line 30-32). 
        /// Alternatively, if a column's connected synapses do not overlap well with any inputs
        /// often enough (as measured by overlapDutyCycle), its permanence values are boosted 
        /// (line 34-36). Note: once learning is turned off, boost(c) is frozen.
        /// Note: the below implementation differs significantly from the Numenta white paper and from the OpenHTM
        /// implementation. The changes were made is order to enforce pattern separation, and to optimize the 
        /// creation of well fitting feature detectors for vision applications. Eventually, it may be better
		/// to replace the proximal synapse learning system with something like the XCAL learning model.
		/// </summary>
        public void PerformBoosting()
        {
            // If this Column's ActiveDutyCycle is less than a small fraction of its MaxDutyCycle (the max ActiveDutyCycle 
            // of the Columns within its InhibitionRadius), then increase its Boost.
            if (this.ActiveDutyCycle < (this.MaxDutyCycle * _increaseBoostThreshold))
            {
                // If this Column hasn't yet reached the specified MaxBoost...
                if ((this.MaxBoost == -1) || (this.Boost < this.MaxBoost))
                {
                    // If this Column's Boost was not increased during the previous time step, meaning that a new period of boosting is beginning...
                    if (this.PrevBoostTime < (this.Region.StepCounter - 1))
                    {
                        // Set the permanence value of each of this Column's connected proximal synapses to exactly ConnectedPerm. This will make it easy
                        // for synapses from inactive inputs to become disconnected the next time this Column is activated, allowing this Column to come to 
                        // represent a smaller subpattern of what it currently represents.
                        foreach (Synapse syn in this.ProximalSegment.Synapses)
                        {
                            if (syn.Permanence > this.Region.ProximalSynapseParams.ConnectedPerm)
                            {
                                syn.Permanence = this.Region.ProximalSynapseParams.ConnectedPerm;
                            }
                        }
                    }

                    // Linearly increase Boost.
                    this.Boost = this.Boost + this.Region.BoostRate;

                    // Limit Boost to MaxBoost (if MaxBoost != -1, which would mean no limit).
                    if (this.MaxBoost != -1)
                    {
                        this.Boost = Math.Min(this.Boost, this.MaxBoost);
                    }

                    // Record when this Boost increase took place.
                    this.PrevBoostTime = this.Region.StepCounter;
                }
                else
                {
                    // Boost the Column's synapse permanences. Unconnected synapses are gradually boosted toward ConnectedPerm. This allows
                    // the Column to potentially come to represent a new pattern.
                    this.BoostPermanences(this.Region.BoostRate);
                }
            }
            else if ((this.Boost > this.MinBoost) && (this.ActiveDutyCycle > (this.MaxDutyCycle * _decreaseBoostThreshold)) && (this.FastActiveDutyCycle > (this.MaxDutyCycle * _decreaseBoostThreshold)))
            {
                // Linearly decrease Boost.
                this.Boost = Math.Max(this.Boost - this.Region.BoostRate, this.MinBoost);
            }
        }

		/// <summary>
        /// Returns the maximum active duty cycle of the columns that are within
		/// inhibitionRadius hypercolumns of this column.
		/// </summary>
		/// <returns>Maximum active duty cycle among neighboring columns.</returns>
        public float DetermineMaxDutyCycle()
        {
            // The below code fails, not sure why. Commented out until I have time to investigate.
            //// If this Column is not the upper-left COlumn in its hypercolumn, return the maxDutyCycle of that upper-left Column. 
            //// All Columns within the same hypercolumns have the same inhbition area and so the same maxDutyCycle.
            //if (((Position.X % region.HypercolumnDiameter) != 0) || ((Position.Y % region.HypercolumnDiameter) != 0)) {
            //	return region.GetColumn(HypercolumnPosition.X * region.HypercolumnDiameter, HypercolumnPosition.Y * region.HypercolumnDiameter).GetMaxDutyCycle();
            //}

            // Find extents of neighboring columns within inhibitionRadius.
            // InhibitionRadius is specified in hypercolumns.
            Area inhibitionArea = this.DetermineColumnsWithinHypercolumnRadius((int)(this.Region.InhibitionRadius + 0.5f));

            // Find the maxDutyCycle of all columns within inhibitionRadius
            float maxDuty = 0.0f;
            for (int x = inhibitionArea.MinX; x <= inhibitionArea.MaxX; x++)
            {
                for (int y = inhibitionArea.MinY; y <= inhibitionArea.MaxY; y++)
                {
                    Column column = this.Region.GetColumn(x, y);
                    if (column.ActiveDutyCycle > maxDuty)
                    {
                        maxDuty = column.ActiveDutyCycle;
                    }
                }
            }

            return maxDuty;
        }

		/// <summary>
        /// Computes a moving average of how often this column has been active 
		/// after inhibition.
		/// </summary>
        public void UpdateActiveDutyCycle()
        {
            // Update ActiveDutyCycle
            float newCycle = (1.0f - _emaAlpha) * this.ActiveDutyCycle;
            if (this.IsActive)
            {
                newCycle += _emaAlpha;
            }
            this.ActiveDutyCycle = newCycle;

            // Update FastActiveDutyCycle
            newCycle = (1.0f - _fastEmaAlpha) * this.FastActiveDutyCycle;
            if (this.IsActive)
            {
                newCycle += _fastEmaAlpha;
            }
            this.FastActiveDutyCycle = newCycle;
        }

		/// <summary>
        /// Computes a moving average of how often this column has overlap greater than
        /// MinOverlap.
        /// Exponential moving average (EMA):
		/// St = a * Yt + (1-a)*St-1
		/// </summary>
        public void UpdateOverlapDutyCycle()
        {
            float newCycle = (1.0f - _emaAlpha) * this.OverlapDutyCycle;

            // CHANGED: Overlap is divided by Boost before being compared with _minOverlap, because _minOverlap doesn't have Boost factored into it. This makes them comparable.
            if ((this.Overlap / this.Boost) >= this.MinOverlap) // Note: Numenta docs indicate >, but given its function in boosting of trying to raise overlap to a number that allows activation (ie., minOverlap), >= makes more sense to me. 
            {
                newCycle += _emaAlpha;
            }
            this.OverlapDutyCycle = newCycle;
        }
    }
}
