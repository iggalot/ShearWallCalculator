using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShearWallCalculator
{
    public class BracedWallLine
    {
        private List<double> values; // Stores the wall positions
        private double tolerance; // Max allowed grouping distance
        private List<List<double>> groupedValues; // Stores computed groups

        public BracedWallLine(double tolerance)
        {
            this.values = new List<double>();
            this.tolerance = tolerance;
            this.groupedValues = new List<List<double>>();
        }

        // Add a value and recompute groups
        public void AddValue(double value)
        {
            values.Add(value);
            RecomputeGroups();
        }

        // Remove a value and recompute groups
        public void RemoveValue(double value)
        {
            values.Remove(value);
            RecomputeGroups();
        }

        // Recomputes the grouping of wall positions
        private void RecomputeGroups()
        {
            if (values.Count == 0)
            {
                groupedValues.Clear();
                return;
            }

            values.Sort(); // Ensure sorted order
            groupedValues.Clear();
            List<double> currentGroup = new List<double> { values[0] };
            double groupStart = values[0]; // First element of the group

            for (int i = 1; i < values.Count; i++)
            {
                if (Math.Abs(values[i] - groupStart) <= tolerance)
                {
                    currentGroup.Add(values[i]);
                }
                else
                {
                    groupedValues.Add(new List<double>(currentGroup));
                    currentGroup.Clear();
                    currentGroup.Add(values[i]);
                    groupStart = values[i];
                }
            }

            if (currentGroup.Count > 0)
                groupedValues.Add(currentGroup);
        }

        // Get the current groups
        public List<List<double>> GetGroups()
        {
            return groupedValues;
        }

        // Display the groups
        public void PrintGroups()
        {
            int groupIndex = 1;
            foreach (var group in groupedValues)
            {
                Console.WriteLine($"Group {groupIndex++}: {string.Join(", ", group)}");
            }
        }

        public void RunTestCase()
        {
            BracedWallLine bracedWallLine = new BracedWallLine(5.0); // Set tolerance

            // Initial values with modifications
            bracedWallLine.AddValue(10);
            bracedWallLine.AddValue(13);
            bracedWallLine.AddValue(15);  // Changed from 17
            bracedWallLine.AddValue(22);  // Changed from 18
            bracedWallLine.AddValue(27);
            bracedWallLine.AddValue(35);
            bracedWallLine.AddValue(37);
            bracedWallLine.AddValue(45);

            // Print results
            Console.WriteLine("After Changing 17 → 15 and 18 → 22:");
            bracedWallLine.PrintGroups();
        }
    }
}
