using ShearWallCalculator.BuildingInfo;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator
{
    public abstract class RoofAreaCalculator_Base
    {
        /// <summary>
        /// Effective wind areas for roof To be overriden by the implementating class
        /// </summary>
        public abstract Dictionary<int, EffectiveWindArea_Roof> effWindAreas_Roof { get; set; }

        /// <summary>
        /// Function to compute the areas of the calculator.  To be overriden by the implementating class
        /// </summary>
        /// <param name="p"></param>
        /// <param name="bldg_data"></param>
        public abstract void Compute(WindLoadParameters_Base p, BuildingData bldg_data);

        public string DisplayResults()
        {
            string str = string.Empty;
            foreach (KeyValuePair<int, EffectiveWindArea_Roof> kvp in effWindAreas_Roof)
            {
                str += kvp.Value.DisplayResults() + "\n";
            }
            Console.WriteLine("=================================");
            str += "\nTotal Roof Area: " + TotalRoofArea();

            return str;
        }

        public double TotalRoofArea()
        {
            double sum = 0;
            foreach (KeyValuePair<int, EffectiveWindArea_Roof> kvp in effWindAreas_Roof)
            {
                sum += kvp.Value.Area;
            }
            return sum;
        }
    }
}
