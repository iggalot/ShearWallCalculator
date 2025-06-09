using calculator;

namespace ShearWallCalculator
{
    public enum ConnectorTypes
    {
        CONNECTOR_HDU = 0,  // HDU_Dict foundation connectors for walls to foundation
        CONNECTOR_HTT = 1,  // HTT_Dict tension connections for upper walls to lower walls
        CONNECTOR_STRAP_TIES = 2, // connections for strapping
    }
    public class ShearWallSelector
    {
        private double Height => Data.WallHeight;
        private double Length => Data.WallLength;
        private double Vu { get; set; } = 0;
        public double? reqHoldDownForce { get => ComputeHolddownForces(); }
        public double? reqHorizShearForce { get => ComputeHorizontalShear(); }
        private WallData Data { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="shear">horizontal force at top of wall in kips</param>
        /// <param name="data">WallData for the wall being designed</param>
        public ShearWallSelector(double shear, WallData data, ConnectorTypes conn_type)
        {
            Data = data;
            Vu = shear;
        }

        public double ComputeHolddownForces()
        {
            return Vu * Height / Length;
        }

        /// <summary>
        /// Computes the horizontal shear requirement for connector(s) on one end of the wall
        /// </summary>
        /// <returns></returns>
        public double ComputeHorizontalShear()
        {
            return Vu / 2;
        }
    }
}
