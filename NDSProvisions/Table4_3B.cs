using System.Collections.Generic;
using System.Reflection;

namespace ShearWallCalculator.NDSProvisions
{
    /// Design parameters for seismic cases
    /// </summary>
    public class Table4_3B_SeismicEntry
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
    public class Table4_3B_WindEntry
    {
        /// <summary>
        /// Edge fastener spacing
        /// </summary>
        public double EdgeFastenerSpacing { get; set; }

        public double v_s { get; set; }  // shear per foot
    }
   
    /// <summary>
    /// Table 4.3B Nominal Unit Shear Capacities for Wood-Frame Shear Walls
    /// Wood Structural Panels Applied over 1/2" or 5/8" Gypsum wallboard or Gypsum sheathing board
    /// </summary>
    public class Table4_3B
    {
        public Table4_3B_SeismicEntry Seismic6;// Case A Seismic -- edge spa = 6,
        public Table4_3B_SeismicEntry Seismic4;// Case A Seismic -- edge spa = 4,
        public Table4_3B_SeismicEntry Seismic3;// Case A Seismic -- edge spa = 3
        public Table4_3B_SeismicEntry Seismic2;// Case A Seismic -- edge spa = 2, 

        public Table4_3B_WindEntry Wind6; // Case B Wind -- edge spa = 6
        public Table4_3B_WindEntry Wind4; // Case B Wind -- edge spa = 4
        public Table4_3B_WindEntry Wind3; // Case B Wind -- edge spa = 3
        public Table4_3B_WindEntry Wind2; // Case B Wind -- edge spa = 2


        public SheathingMaterials SheathingType { get; set; }
        public NominalPanelThicknesses NominalPanelThickness { get; set; }
        public double MinimumFastenerPenetration { get; set; }   // min penetration into framing member or blocking
        public CommonNailSizes CommonNailSize { get; set; }
        public NailTypes NailType { get; set; }
        public string Note { get; set; }   // for storing additional information
    }

    public class Table4_3BManager
    {
        public string Title { get; set; } = "Table 4.3B Nominal Unit Shear Capacities for Wood-Frame Shear Walls";
        public string Description { get; set; } = "Wood Structural Panels Applied over 1/2in. or 5/8in. Gypsum wallboard or Gypsum sheathing board";

        private int _current_id = 0;
        public Dictionary<int, Table4_3B> Table4_3BNominalUnitShearCapacities = new Dictionary<int, Table4_3B>();

        private int GetNextId()
        {
            _current_id++;
            return _current_id;
        }
        public Table4_3BManager()
        {
            BuildTable4_3B();
        }

        private void BuildTable4_3B()
        {
            Table4_3BNominalUnitShearCapacities.Clear();

            // Structural 1 - 8d - 5/16 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 400,
                    G_a_OSB = 13,
                    G_a_PLY = 10
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 600,
                    G_a_OSB = 18,
                    G_a_PLY = 13
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 780,
                    G_a_OSB = 23,
                    G_a_PLY = 16
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1020,
                    G_a_OSB = 35,
                    G_a_PLY = 22
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 840,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1090,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1430,
                },
            });

            // Structural 1 - 10d - 3/8 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                    G_a_OSB = 14,
                    G_a_PLY = 11
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 860,
                    G_a_OSB = 18,
                    G_a_PLY = 14
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1100,
                    G_a_OSB = 24,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1460,
                    G_a_OSB = 37,
                    G_a_PLY = 23
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 785,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1205,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1540,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 2045,
                },
            });

            // Structural 1 - 10d - 7/16 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                    G_a_OSB = 14,
                    G_a_PLY = 11
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 860,
                    G_a_OSB = 18,
                    G_a_PLY = 14
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1100,
                    G_a_OSB = 24,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1460,
                    G_a_OSB = 37,
                    G_a_PLY = 23
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 785,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1205,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1540,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 2045,
                },
            });

            // Structural 1 - 10d - 15/32 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                    G_a_OSB = 14,
                    G_a_PLY = 11
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 860,
                    G_a_OSB = 18,
                    G_a_PLY = 14
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1100,
                    G_a_OSB = 24,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1460,
                    G_a_OSB = 37,
                    G_a_PLY = 23
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 785,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1205,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1540,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 2045,
                },
            });

            // Sheathing - 8d - 5/16 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 360,
                    G_a_OSB = 13,
                    G_a_PLY = 9.5
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 540,
                    G_a_OSB = 18,
                    G_a_PLY = 12
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 700,
                    G_a_OSB = 24,
                    G_a_PLY = 14
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 900,
                    G_a_OSB = 37,
                    G_a_PLY = 18
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 505,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 755,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 980,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1260,
                },
            });

            // Sheathing - 8d - 3/8 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 400,
                    G_a_OSB = 11,
                    G_a_PLY = 8.5
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 600,
                    G_a_OSB = 15,
                    G_a_PLY = 11
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 780,
                    G_a_OSB = 20,
                    G_a_PLY = 13
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1020,
                    G_a_OSB = 32,
                    G_a_PLY = 17
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 560,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 840,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1090,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1430,
                },
            });

            // Sheathing - 10d - 3/8 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 520,
                    G_a_OSB = 13,
                    G_a_PLY = 10
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 760,
                    G_a_OSB = 19,
                    G_a_PLY = 13
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 980,
                    G_a_OSB = 25,
                    G_a_PLY = 15
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1280,
                    G_a_OSB = 39,
                    G_a_PLY = 20
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 730,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1065,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1370,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1790,
                },
            });

            // Sheathing - 10d - 7/16 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 520,
                    G_a_OSB = 13,
                    G_a_PLY = 10
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 760,
                    G_a_OSB = 19,
                    G_a_PLY = 13
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 980,
                    G_a_OSB = 25,
                    G_a_PLY = 15
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1280,
                    G_a_OSB = 39,
                    G_a_PLY = 20
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 730,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1065,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1370,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1790,
                },
            });

            // Sheathing - 10d - 15/32 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_COMMON_BOX_AND_GALVANIZED,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 520,
                    G_a_OSB = 13,
                    G_a_PLY = 10
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 760,
                    G_a_OSB = 19,
                    G_a_PLY = 13
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 980,
                    G_a_OSB = 25,
                    G_a_PLY = 15
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1280,
                    G_a_OSB = 39,
                    G_a_PLY = 20
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 730,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 1065,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 1370,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1790,
                },
            });

            // Plywood - 8d - 5/16 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                NailType = NailTypes.NAILTYPE_GALVANZIED_ONLY,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                Note = "8d (2.5in.x0.113in)",
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 280,
                    G_a_OSB = 13,
                    G_a_PLY = 13
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 420,
                    G_a_OSB = 16,
                    G_a_PLY = 16
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 550,
                    G_a_OSB = 17,
                    G_a_PLY = 17
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 720,
                    G_a_OSB = 21,
                    G_a_PLY = 21
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 390,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 590,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 770,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1010,
                },
            });

            // Plywood - 10d - 3/8 panel
            Table4_3BNominalUnitShearCapacities.Add(GetNextId(), new Table4_3B
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                NailType = NailTypes.NAILTYPE_GALVANZIED_ONLY,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                Note = "8d (3.0in.x0.128in)",
                Seismic6 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 320,
                    G_a_OSB = 16,
                    G_a_PLY = 16
                },
                Seismic4 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 480,
                    G_a_OSB = 18,
                    G_a_PLY = 18
                },
                Seismic3 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 620,
                    G_a_OSB = 20,
                    G_a_PLY = 20
                },
                Seismic2 = new Table4_3B_SeismicEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 820,
                    G_a_OSB = 22,
                    G_a_PLY = 22
                },

                Wind6 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 6,
                    v_s = 450,
                },
                Wind4 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 4,
                    v_s = 670,
                },
                Wind3 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 3,
                    v_s = 870,
                },
                Wind2 = new Table4_3B_WindEntry
                {
                    EdgeFastenerSpacing = 2,
                    v_s = 1150,
                },
            });
        }
    }
}
