using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    /// <summary>
    /// Contains data for a single diaphragm system of multiple diaphragms
    /// </summary>
    public class DiaphragmSystem
    {
        // A collection of all diaphragms defined in the system
        public Dictionary<int, DiaphragmData_Rectangular> _diaphragms { get; set; } = new Dictionary<int, DiaphragmData_Rectangular>(); // collection of walls>

        // center of mass of the diaphragm composite region
        [JsonIgnore]
        public System.Windows.Point CtrMass { get; set; } = new System.Windows.Point(double.NaN, double.NaN);
        [JsonIgnore]
        public float TotalArea { get; set; } = 0.0f;

        /// <summary>
        /// Returns the minimum (leftmost) X value of all diaphragm points
        /// </summary>
        [JsonIgnore]
        public System.Windows.Point X_MIN 
        { 
            get
            {
                if (_diaphragms.Count == 0) return new System.Windows.Point(1000000, 1000000);

                System.Windows.Point minpt = new System.Windows.Point (1000000, 1000000);

                foreach (DiaphragmData_Rectangular item in _diaphragms.Values)
                {
                    System.Windows.Point p1 = item.P1;
                    System.Windows.Point p2 = item.P2;
                    System.Windows.Point p3 = item.P3;
                    System.Windows.Point p4 = item.P4;

                    if (p1.X < minpt.X) minpt = p1;
                    if (p2.X < minpt.X) minpt = p2;
                    if (p3.X < minpt.X) minpt = p3;
                    if (p4.X < minpt.X) minpt = p4;
                }
                return minpt;
            }
        }
        /// <summary>
        /// Returns the maximum (rightmost) X value of all diaphragm points
        /// </summary>
        [JsonIgnore]
        public System.Windows.Point X_MAX
        {
            get
            {
                if (_diaphragms.Count == 0) return new System.Windows.Point(-1000000, -1000000);

                System.Windows.Point maxpt = new System.Windows.Point(-1000000, -1000000);

                foreach (DiaphragmData_Rectangular item in _diaphragms.Values)
                {
                    System.Windows.Point p1 = item.P1;
                    System.Windows.Point p2 = item.P2;
                    System.Windows.Point p3 = item.P3;
                    System.Windows.Point p4 = item.P4;

                    if (p1.X > maxpt.X) maxpt = p1;
                    if (p2.X > maxpt.X) maxpt = p2;
                    if (p3.X > maxpt.X) maxpt = p3;
                    if (p4.X > maxpt.X) maxpt = p4;
                }
                return maxpt;
            }
        }
        /// <summary>
        /// Returns the minimum (bottom) Y value of all diaphragm points
        /// </summary>
        [JsonIgnore]
        public System.Windows.Point Y_MIN
        {
            get
            {
                if (_diaphragms.Count == 0) return new System.Windows.Point(1000000, 1000000);

                System.Windows.Point minpt = new System.Windows.Point(1000000, 1000000);

                foreach (DiaphragmData_Rectangular item in _diaphragms.Values)
                {
                    System.Windows.Point p1 = item.P1;
                    System.Windows.Point p2 = item.P2;
                    System.Windows.Point p3 = item.P3;
                    System.Windows.Point p4 = item.P4;

                    if (p1.Y < minpt.Y) minpt = p1;
                    if (p2.Y < minpt.Y) minpt = p2;
                    if (p3.Y < minpt.Y) minpt = p3;
                    if (p4.Y < minpt.Y) minpt = p4;
                }
                return minpt;
            }
        }
        /// <summary>
        /// Returns the maximum (top) Y value of all diaphragm points
        /// </summary>
        [JsonIgnore]
        public System.Windows.Point Y_MAX
        {
            get
            {
                if (_diaphragms.Count == 0) return new System.Windows.Point(-1000000, -1000000);

                System.Windows.Point maxpt = new System.Windows.Point(-1000000, -1000000);

                foreach (DiaphragmData_Rectangular item in _diaphragms.Values)
                {
                    System.Windows.Point p1 = item.P1;
                    System.Windows.Point p2 = item.P2;
                    System.Windows.Point p3 = item.P3;
                    System.Windows.Point p4 = item.P4;

                    if (p1.Y > maxpt.Y) maxpt = p1;
                    if (p2.Y > maxpt.Y) maxpt = p2;
                    if (p3.Y > maxpt.Y) maxpt = p3;
                    if (p4.Y > maxpt.Y) maxpt = p4;
                }
                return maxpt;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DiaphragmSystem()
        {
            //DiaphragmPoints.Clear();
            Update();
        }

        /// <summary>
        /// updates the calculations for the system.
        /// </summary>
        public void Update()
        {
            foreach (DiaphragmData_Rectangular item in _diaphragms.Values)
            {
                item.Update();
            }

            // Recompute the center of mass  and the total area for the system
            ComputeCenterOfMassAndTotalArea();
        }

        /// <summary>
        /// Compute the center of mass of multipple defined diaphragm regions
        /// </summary>
        public void ComputeCenterOfMassAndTotalArea()
        {
            float sum_x_A = 0;
            float sum_y_A = 0;
            float sum_A = 0;

            foreach (DiaphragmData_Rectangular item in _diaphragms.Values)
            {
                sum_x_A += (float)(item.Area * item.Centroid.X);
                sum_y_A += (float)(item.Area * item.Centroid.Y);
                sum_A += (float)(item.Area);
            }

            if(sum_A == 0)
            {
                CtrMass = new System.Windows.Point(double.NaN, double.NaN);
                TotalArea = 0;
            } else
            {
                CtrMass = new System.Windows.Point(sum_x_A / sum_A, sum_y_A / sum_A);
                TotalArea = sum_A;
            }
        }

        /// <summary>
        /// Adds a wall to the wall system
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wall"></param>
        public void AddDiaphragm(DiaphragmData_Rectangular diaphragm)
        {
            int id = GetNextDiaphragmID();
            _diaphragms.Add(id, diaphragm);
            Update();
        }

        /// <summary>
        /// Removes a wall from the wall system
        /// </summary>
        /// <param name="id"></param>
        public void DeleteDiaphragm(int id)
        {
            bool exists = false;
            if (_diaphragms.ContainsKey(id))
            {
                exists = true;
                _diaphragms.Remove(id);
            }

            if (exists == false)
            {
                Console.WriteLine("\nDiaphragm #" + id.ToString() + " does not exist.");
                return;
            }
            else
            {
                Console.WriteLine("\nDeleting diaphragm #" + id.ToString());
                Update();
            }
        }

        /// <summary>
        /// Scans the list of walls currently stored to find the next available wall id.  This should ensure a unique identifier
        /// </summary>
        /// <returns></returns>
        private int GetNextDiaphragmID()
        {
            int i = 0;
            while (true)
            {
                if (!_diaphragms.ContainsKey(i))
                {
                    return i;
                }
                else
                {
                    i++;
                }
            }
        }
    }
}
