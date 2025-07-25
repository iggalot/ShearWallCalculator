using ShearWallCalculator.BuildingInfo;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator
{
    public abstract class AreaCalculator_Base
    {
        /// <summary>
        /// Effective wind areas for roof To be overriden by the implementating class
        /// </summary>
        public abstract Dictionary<int, EffectiveWindArea> effWindAreas { get; set; }

        public virtual bool HasCritDim { get; set; } = false;
        public virtual double CritDim_a { get; set; } = 0;

        public virtual string Note { get; set; } = String.Empty;  // a holder for a note in the calculator -- useful for recording gable vs. non gable walls

        /// <summary>
        /// Function to compute the areas of the calculator.  To be overriden by the implementating class
        /// </summary>
        /// <param name="p"></param>
        /// <param name="bldg_data"></param>
        public abstract void Compute(WindParameters_Base p, BuildingData bldg_data, Dictionary<string, double> optionalDimension = null);

        public string DisplayResults()
        {
            string str = string.Empty;
            foreach (KeyValuePair<int, EffectiveWindArea> kvp in effWindAreas)
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
            foreach (KeyValuePair<int, EffectiveWindArea> kvp in effWindAreas)
            {
                sum += kvp.Value.Area;
            }
            return sum;
        }
    }
}
