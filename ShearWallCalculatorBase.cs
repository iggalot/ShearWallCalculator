using calculator;
using System.Data;

namespace ShearWallCalculator
{
    public class ShearWallCalculatorBase
    {
        public WallSystem _wall_system = new WallSystem();
        public DiaphragmSystem _diaphragm_system = new DiaphragmSystem();

        public ShearWallCalculatorBase()
        {
            
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="wall_system"></param>
        /// <param name="diaphragm_system"></param>
        public ShearWallCalculatorBase(WallSystem wall_system, DiaphragmSystem diaphragm_system)
        {
            _diaphragm_system = diaphragm_system;
            _wall_system = wall_system;

            LoadTestWallData();
//            LoadTestWallData2();
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
            _wall_system.AddWall(new WallData(9, 0, 0, 20, 0, WallDirs.EastWest));
            _wall_system.AddWall(new WallData(9, 20, 30, 30, 30, WallDirs.EastWest));
            _wall_system.AddWall(new WallData(9, 20, 45, 30, 45, WallDirs.EastWest));
            _wall_system.AddWall(new WallData(9, 0, 75, 20, 75, WallDirs.EastWest));

            // North / South Wall Segments
            _wall_system.AddWall(new WallData(9, 0, 27.5f, 0, 47.5f, WallDirs.NorthSouth));
            _wall_system.AddWall(new WallData(9, 20, 20, 20, 30, WallDirs.NorthSouth));
            _wall_system.AddWall(new WallData(9, 20, 45, 20, 55, WallDirs.NorthSouth));

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
            _wall_system.AddWall(new WallData(9, 15, 80, 25, 80, WallDirs.EastWest));
            _wall_system.AddWall(new WallData(9, 15, 80, 25, 80, WallDirs.EastWest)); // this wall is 3x rigid as others

            // North / South Wall Segments
            _wall_system.AddWall(new WallData(9, 0, 5, 0, 15, WallDirs.NorthSouth));
            _wall_system.AddWall(new WallData(9, 40, 5, 40, 15, WallDirs.NorthSouth));

            _wall_system.Update();
        }

        /// <summary>
        /// Called by subclasses to force an update
        /// </summary>
        public virtual void Update() {}
    }
}
