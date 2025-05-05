using calculator;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Windows;

namespace ShearWallCalculator
{
    public class ShearWallCalculatorBase
    {
        private const double DEFAULT_BUILDING_HEIGHT = 10.0;
        public double building_height { get; set; } = DEFAULT_BUILDING_HEIGHT; // TODO:  need to make this sync with the wind load calculations which can potentially override the value


        public string CalculatorType { get; }
        public WallSystem _wall_system { get; set;  } = new WallSystem();
        public DiaphragmSystem _diaphragm_system { get; set; } = new DiaphragmSystem();

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
        [JsonIgnore]
        public bool IsValidForCalculation { get; set; }

        /// <summary>
        /// Contains a Rect that determines the maximum extents of the wall system and diaphragm system of the model
        /// </summary>
        public Rect BoundingBoxWorld { get; private set; }

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

            //BracedWallLine bracedWallLine = new BracedWallLine(5.0);
            //            LoadTestWallData();
            //            LoadTestWallData2();

            Update();
        }

        public void Update()
        {
            _wall_system.Update();
            _diaphragm_system.Update();

            // set the BoundingBox for the model
            BoundingBoxWorld = ComputeBoundingBox_World();
            building_height = ComputeBuildingHeight();

            // validate the model for calculations
            IsValidForCalculation = Validate();
        }

        /// <summary>
        /// Returns the maximum wall height of the system.
        /// </summary>
        /// <returns></returns>
        private double ComputeBuildingHeight()
        {
            double height = 0.0;
            foreach (KeyValuePair<int, WallData> wall in _wall_system._walls)
            {
                if (wall.Value.WallHeight > height) height = wall.Value.WallHeight;
            }
            return height;
        }

        /// <summary>
        /// Algorithm to determine the maximum and minimum extents of the wall system and the diaphragm system
        /// </summary>
        /// <returns></returns>
        private Rect ComputeBoundingBox_World()
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            if (_wall_system != null)
            {
                foreach (KeyValuePair<int, WallData> wall in _wall_system._walls)
                {
                    if (wall.Value.Start.X < minX) minX = wall.Value.Start.X;
                    if (wall.Value.End.X < minX) minX = wall.Value.End.X;

                    if (wall.Value.Start.Y < minY) minY = wall.Value.Start.Y;
                    if (wall.Value.End.Y < minY) minY = wall.Value.End.Y;

                    if (wall.Value.Start.X > maxX) maxX = wall.Value.Start.X;
                    if (wall.Value.End.X > maxX) maxX = wall.Value.End.X;

                    if (wall.Value.Start.Y > maxY) maxY = wall.Value.Start.Y;
                    if (wall.Value.End.Y > maxY) maxY = wall.Value.End.Y;
                }
            }

            if (_diaphragm_system != null)
            {
                foreach (KeyValuePair<int, DiaphragmData_Rectangular> dia in _diaphragm_system._diaphragms)
                {
                    if (dia.Value.P1.X < minX) minX = dia.Value.P1.X;
                    if (dia.Value.P2.X < minX) minX = dia.Value.P2.X;
                    if (dia.Value.P3.X < minX) minX = dia.Value.P3.X;
                    if (dia.Value.P4.X < minX) minX = dia.Value.P4.X;

                    if (dia.Value.P1.Y < minY) minY = dia.Value.P1.Y;
                    if (dia.Value.P2.Y < minY) minY = dia.Value.P2.Y;
                    if (dia.Value.P3.Y < minY) minY = dia.Value.P3.Y;
                    if (dia.Value.P4.Y < minY) minY = dia.Value.P4.Y;

                    if (dia.Value.P1.X > maxX) maxX = dia.Value.P1.X;
                    if (dia.Value.P2.X > maxX) maxX = dia.Value.P2.X;
                    if (dia.Value.P3.X > maxX) maxX = dia.Value.P3.X;
                    if (dia.Value.P4.X > maxX) maxX = dia.Value.P4.X;

                    if (dia.Value.P1.Y > maxY) maxY = dia.Value.P1.Y;
                    if (dia.Value.P2.Y > maxY) maxY = dia.Value.P2.Y;
                    if (dia.Value.P3.Y > maxY) maxY = dia.Value.P3.Y;
                    if (dia.Value.P4.Y > maxY) maxY = dia.Value.P4.Y;
                }
            }

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
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
    }
}
