using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator
{
    public abstract class AreaCalculator_Base : IEffectiveWindAreaCalculator
    {
        public abstract BuildingData buildingData { get; set; }

        /// <summary>
        /// Effective wind areas for roof To be overriden by the implementating class
        /// </summary>
        public Dictionary<int, EffectiveWindArea> effWindAreas { get; set; } = new Dictionary<int, EffectiveWindArea>();

        public virtual double CritDim_a { get; set; } = 0;
        public virtual bool HasCritDim { get; set; } = false;

        public virtual string Note { get; set; } = String.Empty;  // a holder for a note in the calculator -- useful for recording gable vs. non gable walls

        public virtual void ComputeEffectiveWindAreas(WindParameters_Base p, BuildingData bldg_data, bool windIsParallelToRidge = true, Dictionary<string, double> optionalDimension = null) { }


        /// <summary>
        /// Try to create a zone with positive area.  Otherwise return null;
        /// </summary>
        /// <param name="label"></param>
        /// <param name="outer"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public static EffectiveWindArea TryCreateZone(int id, string label, IEnumerable<Point> outer, IEnumerable<IEnumerable<Point>> holes = null)
        {
            try
            {
                var zone = new EffectiveWindArea(label, outer, holes);
                if (zone.Area > 0)
                {
                    Console.WriteLine($"Zone {id} ({label}) created: Area = {zone.Area:F2} ft²");
                    return zone;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Zone {id} ({label}) failed: {ex.Message}");
            }

            return null;
        }

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
