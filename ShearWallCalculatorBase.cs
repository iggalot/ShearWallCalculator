﻿using calculator;
using System;
using System.Data;
using System.Media;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace ShearWallCalculator
{
    public class ShearWallCalculatorBase
    {
        public WallSystem _wall_system = new WallSystem();
        public DiaphragmSystem _diaphragm_system = new DiaphragmSystem();

        public System.Windows.Point Boundary_Max_Point { get => FindMinBoundaryPt(); }
        public System.Windows.Point Boundary_Min_Point { get => FindMaxBoundaryPt(); }



        /// <summary>
        /// Loads and eccentricty values 
        /// Uses Cartesian coordinate and right-hand rule -- x+ right, y+ up, rot+ = CCW
        /// </summary>
        public double V_x { get; set; } = 1; // x direction load (kips) acting at center of mass
        public double V_y { get; set; } = 1;  // y direction load (kips) acting at center of mass

        /// <summary>
        /// A flag that determines if the calculations can be peformed.  Looks
        /// mainlay at CtrMass and CtrRigidity to make sure both are valid numbers
        /// and not NaN or Infinity
        /// </summary>
        public bool IsValidForCalculation { get => Validate(); }


        private Point FindMaxBoundaryPt()
        {
            float temp_y = -1000000;
            float temp_x = -1000000;

            foreach (var wall in _wall_system._walls.Values)
            {
                if (wall.Start.Y > temp_y) temp_y = (float)wall.Start.Y;
                if (wall.End.Y > temp_y) temp_y = (float)wall.End.Y;
                if (wall.Start.X > temp_x) temp_x = (float)wall.Start.X;
                if (wall.End.X > temp_x) temp_x = (float)wall.End.X;
            }

            foreach (var diaphragm in _diaphragm_system._diaphragms.Values)
            {
                if (diaphragm.P1.Y > temp_y) temp_y = (float)diaphragm.P1.Y;
                if (diaphragm.P1.X > temp_x) temp_x = (float)diaphragm.P1.X;
                if (diaphragm.P2.Y > temp_y) temp_y = (float)diaphragm.P2.Y;
                if (diaphragm.P2.X > temp_x) temp_x = (float)diaphragm.P2.X;
                if (diaphragm.P3.Y > temp_y) temp_y = (float)diaphragm.P3.Y;
                if (diaphragm.P3.X > temp_x) temp_x = (float)diaphragm.P3.X;
                if (diaphragm.P4.Y > temp_y) temp_y = (float)diaphragm.P4.Y;
                if (diaphragm.P4.X > temp_x) temp_x = (float)diaphragm.P4.X;
            }

            return new System.Windows.Point(temp_x, temp_y);
        }

        private Point FindMinBoundaryPt()
        {
            float temp_y = 1000000;
            float temp_x = 1000000;

            foreach (var wall in _wall_system._walls.Values)
            {
                if (wall.Start.Y < temp_y) temp_y = (float)wall.Start.Y;
                if (wall.End.Y < temp_y) temp_y = (float)wall.End.Y;
                if (wall.Start.X < temp_x) temp_x = (float)wall.Start.X;
                if (wall.End.X < temp_x) temp_x = (float)wall.End.X;
            }

            foreach (var diaphragm in _diaphragm_system._diaphragms.Values)
            {
                if (diaphragm.P1.Y < temp_y) temp_y = (float)diaphragm.P1.Y;
                if (diaphragm.P1.X < temp_x) temp_x = (float)diaphragm.P1.X;
                if (diaphragm.P2.Y < temp_y) temp_y = (float)diaphragm.P2.Y;
                if (diaphragm.P2.X < temp_x) temp_x = (float)diaphragm.P2.X;
                if (diaphragm.P3.Y < temp_y) temp_y = (float)diaphragm.P3.Y;
                if (diaphragm.P3.X < temp_x) temp_x = (float)diaphragm.P3.X;
                if (diaphragm.P4.Y < temp_y) temp_y = (float)diaphragm.P4.Y;
                if (diaphragm.P4.X < temp_x) temp_x = (float)diaphragm.P4.X;
            }

            return new System.Windows.Point(temp_x, temp_y);
        }

        public ShearWallCalculatorBase()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="wall_system"></param>
        /// <param name="diaphragm_system"></param>
        public ShearWallCalculatorBase(WallSystem wall_system, DiaphragmSystem diaphragm_system, double shear_x, double shear_y)
        {
            _diaphragm_system = diaphragm_system;
            _wall_system = wall_system;
            V_x = shear_x;
            V_y = shear_y;

            BracedWallLine bracedWallLine = new BracedWallLine(5.0);
            //            LoadTestWallData();
            //            LoadTestWallData2();

            Validate();
        }

        /// <summary>
        /// Checks if the numerics are valid for CtrMass and CtrRigidity so that calculations can be done.
        /// </summary>
        public bool Validate()
        {
            if (_diaphragm_system is null || _wall_system is null ||
                double.IsNaN(_diaphragm_system.CtrMass.X) ||
                double.IsNaN(_diaphragm_system.CtrMass.Y) ||
                double.IsNaN(_wall_system.CtrRigidity.X) ||
                double.IsNaN(_wall_system.CtrRigidity.Y) ||
                double.IsInfinity(_diaphragm_system.CtrMass.X) ||
                double.IsInfinity(_diaphragm_system.CtrMass.Y) ||
                double.IsInfinity(_wall_system.CtrRigidity.X) ||
                double.IsInfinity(_wall_system.CtrRigidity.Y))
            {
                return false;
            } else
            {
                return true; 
            }
        }



        /// <summary>
        /// Prepares a set of test data.  Based on Tonatiuh Rodriquez Niki video 01 on YouTube.
        /// https://www.youtube.com/watch?v=Ljp5M0CTOwA&list=PLOnJNeyZggWT5z5PoXbNfec9nlfH6AczC&index=1
        /// </summary>
        private void LoadTestWallData()
        {
            // Center of mass
            _diaphragm_system.CtrMass = new System.Windows.Point(7.58f, 37.5f);

            // East / West Wall Segments
            _wall_system.AddWall(new WallData(9, 0, 0, 20, 0));
            _wall_system.AddWall(new WallData(9, 20, 30, 30, 30));
            _wall_system.AddWall(new WallData(9, 20, 45, 30, 45));
            _wall_system.AddWall(new WallData(9, 0, 75, 20, 75));

            // North / South Wall Segments
            _wall_system.AddWall(new WallData(9, 0, 27.5f, 0, 47.5f));
            _wall_system.AddWall(new WallData(9, 20, 20, 20, 30));
            _wall_system.AddWall(new WallData(9, 20, 45, 20, 55));

            _wall_system.Update();
        }

        /// <summary>
        /// Prepares a set of test data.  Based on Kestava - Rigid Diaphragm Example on YouTube.
        /// https://www.youtube.com/watch?v=f5xqVsTlpnY
        /// </summary>
        private void LoadTestWallData2()
        {
            // Building 40Wx80L
            // R1 = R2 = R3 = R
            // R4 = 3 x R
            _diaphragm_system.CtrMass = new System.Windows.Point(20, 40);

            // East / West Wall Segments
            _wall_system.AddWall(new WallData(9, 15, 80, 25, 80));
            _wall_system.AddWall(new WallData(9, 15, 80, 25, 80)); // this wall is 3x rigid as others

            // North / South Wall Segments
            _wall_system.AddWall(new WallData(9, 0, 5, 0, 15));
            _wall_system.AddWall(new WallData(9, 40, 5, 40, 15));

            _wall_system.Update();
        }

        /// <summary>
        /// Called by subclasses to force an update
        /// </summary>
        public virtual void Update() {}
    }
}
