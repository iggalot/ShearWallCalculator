using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    /// <summary>
    /// Computes C&C pressures for ASCE 7-16 using the analytical method of Part 3 on page 350
    /// </summary>
    public class WindLoadCalculator_CC_ASCE7_16: WindLoadCalculator_Base
    {
        public double Lambda { get; set; } = 1.0;





        public WindLoadCalculator_CC_ASCE7_16(WindLoadParameters p)
        {
            Parameters = p;
            Lambda = GetAdjustmentFactorForBuildingHeightAndExposure(p.MeanRoofHeight, p.ExposureCategory);
        }

        /// <summary>
        /// Gets the lambda ajustment factor for building height and exposure
        /// from Figure 30.4-1 pg. 362 of ASCE 7-16
        /// -- This value is no longer used in ASCE 7-22 and is replaced by a new value
        /// </summary>
        /// <param name="buildingHeight"></param>
        /// <param name="exposureCategory"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private double GetAdjustmentFactorForBuildingHeightAndExposure(double buildingHeight, WindExposureCategories exposureCategory)
        {
            switch (exposureCategory)
            {
                case WindExposureCategories.WIND_EXP_CAT_B:
                    {
                        if (buildingHeight <= 0)
                            throw new NotImplementedException("ERROR: in GetAdjustmentFactorForBuildingHeightAndExposure():  building height, " + buildingHeight + ", cannot be negative or 0");

                        if (buildingHeight < 15) return 0.82;
                        if (buildingHeight < 20) return 0.89;
                        if (buildingHeight < 25) return 0.95;  // interpolated from table
                        if (buildingHeight < 30) return 1.0;
                        if (buildingHeight < 35) return 1.05;
                        if (buildingHeight < 40) return 1.09;
                        if (buildingHeight < 45) return 1.12;
                        if (buildingHeight < 50) return 1.16;
                        if (buildingHeight < 55) return 1.19;
                        if (buildingHeight < 60) return 1.22;

                        // otherwise
                        throw new NotImplementedException("ERROR: in GetAdjustmentFactorForBuildingHeightAndExposure():  building height, " + buildingHeight + ", is greater than 60ft.");
                    }
                case WindExposureCategories.WIND_EXP_CAT_C:
                    if (buildingHeight <= 0)
                        throw new NotImplementedException("ERROR: in GetAdjustmentFactorForBuildingHeightAndExposure():  building height, " + buildingHeight + ", cannot be negative or 0");

                    if (buildingHeight < 15) return 1.21;
                    if (buildingHeight < 20) return 1.29;
                    if (buildingHeight < 25) return 1.35;  // interpolated from table
                    if (buildingHeight < 30) return 1.40;
                    if (buildingHeight < 35) return 1.45;
                    if (buildingHeight < 40) return 1.49;
                    if (buildingHeight < 45) return 1.53;
                    if (buildingHeight < 50) return 1.56;
                    if (buildingHeight < 55) return 1.59;
                    if (buildingHeight < 60) return 1.62;

                    // otherwise
                    throw new NotImplementedException("ERROR: in GetAdjustmentFactorForBuildingHeightAndExposure():  building height, " + buildingHeight + ", is greater than 60ft.");

                case WindExposureCategories.WIND_EXP_CAT_D:
                    if (buildingHeight <= 0)
                        throw new NotImplementedException("ERROR: in GetAdjustmentFactorForBuildingHeightAndExposure():  building height, " + buildingHeight + ", cannot be negative or 0");

                    if (buildingHeight < 15) return 1.47;
                    if (buildingHeight < 20) return 1.55;
                    if (buildingHeight < 25) return 1.61;  // interpolated from table
                    if (buildingHeight < 30) return 1.66;
                    if (buildingHeight < 35) return 1.70;
                    if (buildingHeight < 40) return 1.74;
                    if (buildingHeight < 45) return 1.78;
                    if (buildingHeight < 50) return 1.81;
                    if (buildingHeight < 55) return 1.84;
                    if (buildingHeight < 60) return 1.87;

                    // otherwise
                    throw new NotImplementedException("ERROR: in GetAdjustmentFactorForBuildingHeightAndExposure():  building height, " + buildingHeight + ", is greater than 60ft.");

                default:
                    throw new NotImplementedException("ERROR: in GetAdjustmentFactorForBuildingHeightAndExposure():  exposure category, " + exposureCategory + ", is not supported.");
            }
        }

        /// <summary>
        /// Gets the external pressure coefficients GCp for flat roofs (theta < 7 degrees)
        /// from Figure 30.5-1 pg. 363 of ASCE 7-16
        /// </summary>
        private void GetGCp_FlatRoof_Fig30_5_1()
        {
            // Zone 1:
            // Ae = 1 --> GCp = -1.4
            // Ae = 10 --> GCp = -1.4
            // Ae = 500 --> Gcp = -0.9
            // Ae = 1000 --> GCp = -0.9

            // Zone 2:
            // Ae = 1 --> GCp = -2.3
            // Ae = 10 --> GCp = -2.3
            // Ae = 500 --> Gcp = -1.6
            // Ae = 1000 --> GCp = -1.6

            // Zone 3:
            // Ae = 1 --> GCp = -3.2
            // Ae = 10 --> GCp = -3.2
            // Ae = 500 --> Gcp = -2.3
            // Ae = 1000 --> GCp = -2.3

            // Zone 4 Negative:
            // Ae = 1 --> GCp = -0.9
            // Ae = 20 --> GCp = -0.9
            // Ae = 500 --> Gcp = -0.7
            // Ae = 1000 --> GCp = -0.7

            // Zone4 Positive:
            // Ae = 1 --> GCp = +0.9
            // Ae = 20 --> GCp = +0.9
            // Ae = 500 --> Gcp = +0.6
            // Ae = 1000 --> GCp = +0.6

            // Zone 5 Negative:
            // Ae = 1 --> GCp = -1.8
            // Ae = 20 --> GCp = -1.8
            // Ae = 500 --> Gcp = -1.0
            // Ae = 1000 --> GCp = -1.0

            // Zone 5 Positive:
            // Ae = 1 --> GCp = +0.9
            // Ae = 20 --> GCp = +0.9
            // Ae = 500 --> Gcp = +0.6
            // Ae = 1000 --> GCp = +0.6
        }

        /// <summary>
        /// Gets the external pressure coefficients GCp for gable roofs (theta > 7 degrees)
        /// from Figure 30.3-1 pg. 335 of ASCE 7-16
        /// </summary>
        private void GetGCp_Gable_Walls()
        {

            // Zone 4 Negative:
            // Ae = 1 --> GCp = -1.1
            // Ae = 10 --> GCp = -1.1
            // Ae = 500 --> Gcp = -0.8
            // Ae = 1000 --> GCp = -0.8

            // Zone4 Positive:
            // Ae = 1 --> GCp = +1.0
            // Ae = 10 --> GCp = +1.0
            // Ae = 500 --> Gcp = +0.7
            // Ae = 1000 --> GCp = +0.7

            // Zone 5 Negative:
            // Ae = 1 --> GCp = -1.4
            // Ae = 10 --> GCp = -1.4
            // Ae = 500 --> Gcp = -0.8
            // Ae = 1000 --> GCp = -0.8

            // Zone 5 Positive:
            // Ae = 1 --> GCp = +1.0
            // Ae = 10 --> GCp = +1.0
            // Ae = 500 --> Gcp = +0.7
            // Ae = 1000 --> GCp = +0.7
        }
    }
}
