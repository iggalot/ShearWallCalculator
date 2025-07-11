using ShearWallCalculator.WindLoadCalculations;
using System;

namespace ShearWallCalculator.BuildingInfo
{
    /// <summary>
    /// A class for storing the building data
    /// </summary>
    public class BuildingData
    {
        public double BuildingLength { get; set; } = 60;
        public double BuildingWidth { get; set; } = 60;
        public double BuildingHeight { get; set; } = 10;
        public double RoofPitch { get; set; } = 0;
        public string RidgeDirection { get; set; } = string.Empty;
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_FLAT;

        /// <summary>
        /// The mean roof height of the building, h per ASCE7
        /// </summary>
        public double MeanRoofHeight { get => ComputeMeanRoofHeight(); }

        private double ComputeMeanRoofHeight()
        {
            if (RoofType == RoofTypes.ROOF_TYPE_HIP)
            {
                double h1 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                double h2 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                return BuildingHeight + Math.Max(h1, h2);
            }
            else if (RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                double h1 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                double h2 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                return BuildingHeight + Math.Min(h1, h2);
            } 
            else if (RoofType == RoofTypes.ROOF_TYPE_FLAT)
            {
                return BuildingHeight;
            }
            else
            {
                throw new NotImplementedException("ERROR: In ComputeMeanRoofHeight(), Roof type " + RoofType + " is not implemented");

            }
        }
    }
}
