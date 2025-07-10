using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator
{
    public class RoofAreaCalculator_Base
    {
        /// <summary>
        /// Effective wind areas for roof
        /// </summary>
        public static Dictionary<int, EffectiveWindArea_Roof> effWindAreas_Roof { get; set; } = new Dictionary<int, EffectiveWindArea_Roof>();

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
