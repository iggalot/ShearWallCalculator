using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30_CC.AreaCalculator.ASCE7_22
{
    public class WallAreaCalculator_GableEnd_CC_ASCE7_16 : AreaCalculator_Base
    {
        public override Dictionary<int, EffectiveWindArea> effWindAreas { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public override double CritDim_a { get; set; }
        public override bool HasCritDim { get; set; }

        public override void Compute(WindLoadParameters_Base p, BuildingData bldg_data, Dictionary<string, double> optionalParams = null)
        {
            /// <summary>
            /// The critical width dimenstion "a" used throughout chapter 30
            /// -- minimum of 0.4 * building height and 0.1 * min(building Length, building width)
            /// but not less than 4% of smallest dimension or 3 ft.
            /// </summary>
            CritDim_a = Math.Max(
                Math.Min(0.4 * bldg_data.MeanRoofHeight, 0.1 * Math.Min(bldg_data.BuildingLength, bldg_data.BuildingWidth)),
                Math.Max(0.04 * Math.Min(bldg_data.BuildingLength, bldg_data.BuildingWidth),
                3)
                );
            HasCritDim = true;

            double length;
            if (optionalParams.ContainsKey("WallLength"))
            {
                length = optionalParams["WallLength"];
            }
            else
            {
                throw new Exception("ERROR: Wall length is required in WallAreaCalculator_GableEnd_CC_ASCE7_22 constructor.");
            }

            // Corners of the wall planes -- assumed to be perpendicular to wind
            Point A = new Point(0, 0);
            Point B = new Point(length, 0);
            Point C = new Point(bldg_data.BuildingLength, bldg_data.BuildingHeight);
            Point D = new Point(0, bldg_data.BuildingHeight);

            Point p1 = new Point(CritDim_a, 0);
            Point p2 = new Point(CritDim_a, bldg_data.BuildingHeight);
            Point p3 = new Point(bldg_data.BuildingLength - CritDim_a, 0);
            Point p4 = new Point(bldg_data.BuildingLength - CritDim_a, bldg_data.BuildingHeight);

            effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Point> { A, p1, p2, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { p1, p3, p4, p2 }, null));
            effWindAreas.Add(3, new EffectiveWindArea("Zone5", new List<Point> { p3, B, C, p4 }, null));
        }
    }
}
