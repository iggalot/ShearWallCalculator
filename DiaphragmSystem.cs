using calculator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShearWallCalculator
{
    /// <summary>
    /// Contains data for a single diaphragm system of multiple diaphragms
    /// </summary>
    public class DiaphragmSystem
    {
        // A collection of all walls defined in the system
        public Dictionary<int, DiaphragmData_Rectangular> _diaphragms { get; set; } = new Dictionary<int, DiaphragmData_Rectangular>(); // collection of walls>

        // center of mass of the diaphragm composite region
        public System.Windows.Point CtrMass { get; set; } = new Point(0, 0);

        // collection of points defining thediaphragm outline.  Used for find center of mass
        // -- must be in sequential order (i.e. don't skip points and add them later)
        public List<System.Windows.Point> DiaphragmPoints { get; set; } = new List<System.Windows.Point>();


        public DiaphragmSystem()
        {
            //DiaphragmPoints.Clear();
            Update();
        }

        public void Update()
        {
            // Recompute the center of mass for the region
            ComputeCenterOfMass();
        }

        /// <summary>
        /// Compute the center of mass of multipple defined diaphragm regions
        /// </summary>
        public void ComputeCenterOfMass()
        {
            float x = 0;
            float y = 0;

            //if (DiaphragmPoints.Count == 0)
            //{
            //    x = 0;
            //    y = 0;
            //}
            //else
            //{
            //    int num_pts = DiaphragmPoints.Count;
            //    // find coords
            //    float sum_x = 0;
            //    float sum_y = 0;
            //    for (int i = 0; i < num_pts; i++)
            //    {
            //        sum_x += (float)DiaphragmPoints[i].X;
            //        sum_y += (float)DiaphragmPoints[i].Y;
            //    }
            //    CtrMass = new System.Windows.Point(sum_x / num_pts, sum_y / num_pts);
            //}
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
