using System.Collections.Generic;

namespace ShearWallCalculator.NDSProvisions
{
    /// Design parameters for seismic cases
    /// </summary>
    public class Table4_3_SeismicEntry
    {
        /// <summary>
        /// Edge fastener spacing
        /// </summary>
        public double EdgeFastenerSpacing { get; set; }

        public double v_s { get; set; }  // shear per foot
        public double G_a_OSB { get; set; } // G_a for OSB
        public double G_a_PLY { get; set; } // G_a for PLY
    }

    /// <summary>
    /// Design parameters for wind cases
    /// </summary>
    public class Table4_3_WindEntry
    {
        /// <summary>
        /// Edge fastener spacing
        /// </summary>
        public double EdgeFastenerSpacing { get; set; }

        public double v_s { get; set; }  // shear per foot
    }

   
    /// <summary>
    /// Table 4.3A Nominal Unit Shear Capacities for Wood-Frame Shear Walls
    /// Wood based panels
    /// </summary>
    public class Table4_3A
    {
        public Table4_3_SeismicEntry Seismic6;// Case A Seismic -- edge spa = 6,
        public Table4_3_SeismicEntry Seismic4;// Case A Seismic -- edge spa = 4,
        public Table4_3_SeismicEntry Seismic3;// Case A Seismic -- edge spa = 3
        public Table4_3_SeismicEntry Seismic2;// Case A Seismic -- edge spa = 2, 

        public Table4_3_WindEntry Wind6; // Case B Wind -- edge spa = 6
        public Table4_3_WindEntry Wind4; // Case B Wind -- edge spa = 4
        public Table4_3_WindEntry Wind3; // Case B Wind -- edge spa = 3
        public Table4_3_WindEntry Wind2; // Case B Wind -- edge spa = 2


        public SheathingMaterials SheathingType { get; set; }
        public NominalPanelThicknesses NominalPanelThickness { get; set; }
        public double MinimumFastenerPenetration { get; set; }   // min penetration into framing member or blocking
        public CommonNailSizes CommonNailSize { get; set; }
        public NailTypes NailType { get; set; }
        public string Note { get; set; }   // for storing additional information
    }

    public class Table4_3Manager
    {
        private int _current_id = 0;
        public Dictionary<int, Table4_3A> Table4_3ANominalUnitShearCapacities = new Dictionary<int, Table4_3A>();

        private int GetNextId()
        {
            _current_id++;
            return _current_id;
        }
        public Table4_3Manager()
        {
            BuildTable4_3();
        }

        private void BuildTable4_3()
        {
            Table4_3ANominalUnitShearCapacities.Clear();
            // Structural 1 - 6d - 5/16 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                   EdgeFastenerSpacing = 6,
                   v_s = 400,
                   G_a_OSB = 13,
                   G_a_PLY = 10
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 600,
                    G_a_OSB = 18,
                    G_a_PLY = 13
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 780,
                    G_a_OSB = 23,
                    G_a_PLY = 16
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1020,
                    G_a_OSB = 35,
                    G_a_PLY = 22
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 840,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1090,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1430,
                },
            });
 
            // Structural 1 - 8d - 3/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 460,
                    G_a_OSB = 19,
                    G_a_PLY = 14
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 720,
                    G_a_OSB = 24,
                    G_a_PLY = 17
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 920,
                    G_a_OSB = 30,
                    G_a_PLY = 20
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1220,
                    G_a_OSB = 43,
                    G_a_PLY = 24
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 645,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1010,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1290,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1710,
                },
            });

            // Structural 1 - 8d - 7/16 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 510,
                    G_a_OSB = 16,
                    G_a_PLY = 13
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 790,
                    G_a_OSB = 21,
                    G_a_PLY = 16
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1010,
                    G_a_OSB = 27,
                    G_a_PLY = 19
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1340,
                    G_a_OSB = 40,
                    G_a_PLY = 24
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 715,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1105,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1415,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1875,
                },
            });

            // Structural 1 - 8d - 15/32 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                    G_a_OSB = 14,
                    G_a_PLY = 11
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 860,
                    G_a_OSB = 18,
                    G_a_PLY = 14
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1100,
                    G_a_OSB = 24,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1460,
                    G_a_OSB = 37,
                    G_a_PLY = 23
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 785,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1205,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1540,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 2045,
                },
            });

            // Structural 1 - 10d - 15/32 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 680,
                    G_a_OSB = 22,
                    G_a_PLY = 16
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1020,
                    G_a_OSB = 29,
                    G_a_PLY = 20
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1330,
                    G_a_OSB = 36,
                    G_a_PLY = 22
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1740,
                    G_a_OSB = 51,
                    G_a_PLY = 28
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 950,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1430,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1860,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 2435,
                },
            });

            // Sheathing - 6d - 5/16 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 360,
                    G_a_OSB = 13,
                    G_a_PLY = 9.5
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 540,
                    G_a_OSB = 18,
                    G_a_PLY = 12
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 700,
                    G_a_OSB = 24,
                    G_a_PLY = 14
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 900,
                    G_a_OSB = 37,
                    G_a_PLY = 18
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 505,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 755,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 980,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1260,
                },
            });

            // Sheathing - 6d - 3/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 400,
                    G_a_OSB = 11,
                    G_a_PLY = 8.5
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 600,
                    G_a_OSB = 15,
                    G_a_PLY = 11
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 780,
                    G_a_OSB = 20,
                    G_a_PLY = 13
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1020,
                    G_a_OSB = 32,
                    G_a_PLY = 17
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 840,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1090,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1430,
                },
            });

            // Sheathing - 8d - 3/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 440,
                    G_a_OSB = 17,
                    G_a_PLY = 12
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 640,
                    G_a_OSB = 25,
                    G_a_PLY = 15
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 820,
                    G_a_OSB = 31,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1060,
                    G_a_OSB = 45,
                    G_a_PLY = 20
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 615,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 895,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1150,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1485,
                },
            });

            // Sheathing - 8d - 7/16 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 480,
                    G_a_OSB = 15,
                    G_a_PLY = 11
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 700,
                    G_a_OSB = 22,
                    G_a_PLY = 14
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 900,
                    G_a_OSB = 28,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1170,
                    G_a_OSB = 42,
                    G_a_PLY = 21
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 670,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 980,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1260,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1640,
                },
            });

            // Sheathing - 8d - 15/32 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 520,
                    G_a_OSB = 13,
                    G_a_PLY = 10
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 760,
                    G_a_OSB = 19,
                    G_a_PLY = 13
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 980,
                    G_a_OSB = 25,
                    G_a_PLY = 15
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1280,
                    G_a_OSB = 39,
                    G_a_PLY = 20
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 730,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1065,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1370,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1790,
                },
            });

            // Sheathing - 10d - 15/32 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 620,
                    G_a_OSB = 22,
                    G_a_PLY = 14
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 920,
                    G_a_OSB = 30,
                    G_a_PLY = 17
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1200,
                    G_a_OSB = 37,
                    G_a_PLY = 19
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1540,
                    G_a_OSB = 52,
                    G_a_PLY = 23
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 870,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1290,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1680,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 2155,
                },
            });

            // Sheathing - 10d - 19/32 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 680,
                    G_a_OSB = 19,
                    G_a_PLY = 13
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1020,
                    G_a_OSB = 26,
                    G_a_PLY = 16
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1330,
                    G_a_OSB = 33,
                    G_a_PLY = 18
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1740,
                    G_a_OSB = 48,
                    G_a_PLY = 22
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 950,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1430,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1860,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 2435,
                },
            });

            // Plywood - 6d - 5/16 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_PLYWOOD,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                NailType = NailTypes.NAILTYPE_GALVANZIED_ONLY,

                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 280,
                    G_a_OSB = 13,
                    G_a_PLY = 13
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 420,
                    G_a_OSB = 16,
                    G_a_PLY = 16
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 550,
                    G_a_OSB = 17,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 720,
                    G_a_OSB = 21,
                    G_a_PLY = 21
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 390,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 590,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 770,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1010,
                },
            });

            // Plywood - 8d - 3/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_PLYWOOD,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_GALVANZIED_ONLY,

                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 320,
                    G_a_OSB = 16,
                    G_a_PLY = 16
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 480,
                    G_a_OSB = 18,
                    G_a_PLY = 18
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 620,
                    G_a_OSB = 20,
                    G_a_PLY = 20
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 820,
                    G_a_OSB = 22,
                    G_a_PLY = 22
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 450,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 670,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 870,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1150,
                },
            });

            // Particleboard - 6d - 3/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_PARTICLEBOARD,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 0,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Note = "M-S Exterior Glue or M-2 Exterior Glue",
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 240,
                    G_a_OSB = 15,
                    G_a_PLY = 15
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 360,
                    G_a_OSB = 17,
                    G_a_PLY = 17
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 460,
                    G_a_OSB = 19,
                    G_a_PLY = 19
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 600,
                    G_a_OSB = 22,
                    G_a_PLY = 22
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 335,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 505,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 645,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 840,
                },
            });

            // Particleboard - 8d - 3/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_PARTICLEBOARD,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 0,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Note = "M-S Exterior Glue or M-2 Exterior Glue",
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 260,
                    G_a_OSB = 18,
                    G_a_PLY = 18
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 380,
                    G_a_OSB = 20,
                    G_a_PLY = 20
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 480,
                    G_a_OSB = 21,
                    G_a_PLY = 21
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 630,
                    G_a_OSB = 23,
                    G_a_PLY = 23
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 365,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 530,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 670,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 880,
                },
            });

            // Particleboard - 8d - 1/2 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_PARTICLEBOARD,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 0,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                Note = "M-S Exterior Glue or M-2 Exterior Glue",
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 280,
                    G_a_OSB = 18,
                    G_a_PLY = 18
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 420,
                    G_a_OSB = 20,
                    G_a_PLY = 20
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 540,
                    G_a_OSB = 22,
                    G_a_PLY = 22
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 700,
                    G_a_OSB = 24,
                    G_a_PLY = 24
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 390,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 590,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 755,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 980,
                },
            });

            // Particleboard - 10d - 1/2 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_PARTICLEBOARD,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 0,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                Note = "M-S Exterior Glue or M-2 Exterior Glue",
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 370,
                    G_a_OSB = 21,
                    G_a_PLY = 21
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 550,
                    G_a_OSB = 23,
                    G_a_PLY = 23
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 720,
                    G_a_OSB = 24,
                    G_a_PLY = 24
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 920,
                    G_a_OSB = 25,
                    G_a_PLY = 25
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 520,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 770,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1010,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1290,
                },
            });

            // Particleboard - 10d - 5/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_PARTICLEBOARD,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,

                MinimumFastenerPenetration = 0,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN,
                Note = "M-S Exterior Glue or M-2 Exterior Glue",
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 400,
                    G_a_OSB = 21,
                    G_a_PLY = 21
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 610,
                    G_a_OSB = 23,
                    G_a_PLY = 23
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 790,
                    G_a_OSB = 24,
                    G_a_PLY = 24
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1040,
                    G_a_OSB = 26,
                    G_a_PLY = 26
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 855,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1105,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1455,
                },
            });

            // Fiberboard - 11GA - 1/2 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_FIBERBOARD,
                CommonNailSize = CommonNailSizes.NAILSIZE_11GA_1_75LONG_7_16HEAD,
                NailType = NailTypes.NAILTYPE_GALVANIZED_ROOF,

                MinimumFastenerPenetration = 0,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                Note = "",
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6, // This isn't an option for this material
                    v_s = 0,   
                    G_a_OSB = 0,
                    G_a_PLY = 0
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 340,
                    G_a_OSB = 4.0,
                    G_a_PLY = 4.0
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 460,
                    G_a_OSB = 5.0,
                    G_a_PLY = 5.0
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 520,
                    G_a_OSB = 5.5,
                    G_a_PLY = 5.5
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6, // This isn't an option for this material
                    v_s = 0,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 475,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 645,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 730,
                },
            });

            // Fiberboard - 11GA - 5/8 panel
            Table4_3ANominalUnitShearCapacities.Add(GetNextId(), new Table4_3A
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_FIBERBOARD,
                CommonNailSize = CommonNailSizes.NAILSIZE_11GA_1_75LONG_3_8HEAD,
                NailType = NailTypes.NAILTYPE_GALVANIZED_ROOF,

                MinimumFastenerPenetration = 0,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_25_32IN,
                Note = "",
                Seismic6 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 6, // This isn't an option for this material
                    v_s = 0,
                    G_a_OSB = 0,
                    G_a_PLY = 0
                },
                Seismic4 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 340,
                    G_a_OSB = 4.0,
                    G_a_PLY = 4.0
                },
                Seismic3 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 460,
                    G_a_OSB = 5.0,
                    G_a_PLY = 5.0
                },
                Seismic2 = new Table4_3_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 520,
                    G_a_OSB = 5.5,
                    G_a_PLY = 5.5
                },

                Wind6 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 6, // This isn't an option for this material
                    v_s = 0,
                },
                Wind4 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 475,
                },
                Wind3 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 645,
                },
                Wind2 = new Table4_3_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 730,
                },
            });
        }
    }
}
