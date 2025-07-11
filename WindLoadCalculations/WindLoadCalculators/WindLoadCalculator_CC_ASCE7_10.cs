using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    /// <summary>
    /// Computes C&C pressures for ASCE 7-16 using the analytical method of Part 3 on page 350
    /// </summary>
    public class WindLoadCalculator_CC_ASCE7_10: WindLoadCalculator_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_10; }

        // Which figure of Ch30_3_2A thru I to use for CC
        public Chapter30_BaseFigure extGCpCurve{get; set;}

        /// <summary>
        /// The critical width dimenstion "a" used throughout chapter 30
        /// -- minimum of 0.4 * building height and 0.1 * min(building Length, building width)
        /// </summary>
        public double CritDim_a
        {
            get
            {
                return Math.Min(0.4 * buildingData.MeanRoofHeight, 0.1 * Math.Min(buildingData.BuildingLength, buildingData.BuildingWidth));
            }
        }

        public WindLoadCalculator_CC_ASCE7_10(WindLoadParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;

            //if (p.MeanRoofHeight <= 60)
            //{

            //    switch (p.RoofType)
            //    {
            //        case RoofTypes.ROOF_TYPE_FLAT:
            //            GetExtGCpCurve_FlatRoof(p);
            //            break;
            //        case RoofTypes.ROOF_TYPE_GABLE:
            //            GetExtGCpCurve_GableRoof(p);
            //            break;
            //        case RoofTypes.ROOF_TYPE_HIP:
            //            GetExtGCpCurve_HipRoof(p);
            //            break;
            //        default:
            //            throw new Exception("ERROR: Invalid roof type: " + p.RoofType + " in WindLoadCalculator_CC_ASCE7_16 constructor.");
            //    }
            //} 

            //foreach (var kvp in Parameters.effWindAreas_Roof)
            //{
            //    Console.WriteLine(kvp.Key + " " + kvp.Value.Area+"\n");
            //    //double val = ExternalGCpCurve.GetGCp(kvp.Value.Area);
            //}
        }




        //private void GetExtGCpCurve_FlatRoof(WindLoadParameters_Base p)
        //{
        //    extGCpCurve = new Figure30_3_2A();
        //}

        //private void GetExtGCpCurve_GableRoof(WindLoadParameters_Base p)
        //{
        //    if (p.RoofPitch < 0)
        //    {
        //        throw new NotImplementedException("Error: Roof slope is less than 0 degrees. No table defined in GetExtGCpCuve_GableRoof().");
        //    }
        //    else if (p.RoofPitch <= 7)
        //    {
        //        extGCpCurve = new Figure30_3_2A();
        //    }
        //    else if (p.RoofPitch <= 20)
        //    {
        //        extGCpCurve = new Figure30_3_2B();
        //    }
        //    else if (p.RoofPitch <= 27)
        //    {
        //        extGCpCurve = new Figure30_3_2C();
        //    }
        //    else if (p.RoofPitch <= 45)
        //    {
        //        extGCpCurve = new Figure30_3_2D();
        //    }
        //    else
        //    {
        //        throw new NotImplementedException("Error: Roof slope is greater than 45 degrees. No table defined in GetExtGCpCuve_GableRoof().");
        //    }
        //}

        //private void GetExtGCpCurve_HipRoof(WindLoadParameters_Base p)
        //{
        //    if (p.RoofPitch < 0)
        //    {
        //        throw new NotImplementedException("Error: Roof slope is less than 0 degrees. No table defined in GetExtGCpCuve_HipRoof().");
        //    }
        //    else if (p.RoofPitch <= 7)
        //    {
        //        extGCpCurve = new Figure30_3_2A();
        //    }
        //    else if (p.RoofPitch <= 20)
        //    {
        //        extGCpCurve = new Figure30_3_2E_2F(p.MeanRoofHeight, p.BuildingWidth);
        //    }
        //    else if (p.RoofPitch <= 27)
        //    {
        //        extGCpCurve = new Figure30_3_2G(p.MeanRoofHeight, p.BuildingWidth);
        //    }
        //    else if (p.RoofPitch <= 45)
        //    {
        //        extGCpCurve = new Figure30_3_2H_2I(p.MeanRoofHeight, p.BuildingWidth, p.RoofPitch);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException("Error: Roof slope is greater than 45 degrees. No table defined in GetExtGCpCuve_HipRoof().");
        //    }
        //}
    }
}
