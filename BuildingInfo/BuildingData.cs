using ShearWallCalculator.WindLoadCalculations;
using System;

namespace ShearWallCalculator.BuildingInfo
{
    public enum BuildingEnclosures
    {
        BLDG_ENCLOSED = 0,
        BLDG_PARTIALLY_ENCLOSED = 1,
        BLDG_PARTIALLY_OPEN = 2,
        BLDG_OPEN = 3
    }
    
    // <summary>
    /// A class for storing the building data
    /// </summary>
    public class BuildingData
    {
        public double BuildingLength { get; set; } = 60; // L
        public double BuildingWidth { get; set; } = 40;  // B
        public double BuildingHeight { get; set; } = 15;
        public double RoofPitch { get; set; } = 15;
        public string RidgeDirection { get; set; } = string.Empty;
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_GABLE;
        public BuildingEnclosures EnclosureType { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;

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
                return 0.5*(BuildingHeight + BuildingHeight + Math.Min(h1, h2))  // mean roof height is average of the peak height and the wall height
                    ;
            }
            else if (RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                double h1 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                double h2 = Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                return 0.5 * (BuildingHeight + BuildingHeight + Math.Min(h1, h2)); // // mean roof height is average of the peak height and the wall height
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
