using System;
using System.Collections.Generic;

namespace ShearWallCalculator.NDSProvisions
{
    /// <summary>
    /// Enumerations for the NDS 4.2
    /// </summary>
    public enum SheathingOrientations
    {
        ORIENTATION_CASE_1 = 1,
        ORIENTATION_CASE_2 = 2,
        ORIENTATION_CASE_3 = 3,
        ORIENTATION_CASE_4 = 4,
        ORIENTATION_CASE_5 = 5,
        ORIENTATION_CASE_6 = 6
    }

    public enum SheathingGrades
    {
        SHEATH_GADE_STRUCTURAL_1 = 0,
        SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR = 1
    }

    public enum DiaphragmTypes
    {
        DIAPHRAGM_OSB = 0,
        DIAPHRAGM_3_PLY = 1,
        DIAPHRAGM_4_PLY = 2,   // Gs values multipled by 1.2
        DIAPHRAGM_5_PLY = 3    // Gs values multipled by 1.2
    }
    public enum CommonNailSizes
    {
        NAILSIZE_6D = 0,
        NAILSIZE_8D = 1,
        NAILSIZE_10D = 2
    }

    public enum NominalPanelThicknesses
    {
        THICKNESS_5_16IN = 0,
        THICKNESS_3_8IN = 1,
        THICKNESS_7_16IN = 2,
        THICKNESS_15_32IN = 3,
        THICKNESS_19_32IN = 4
    }

    /// <summary>
    /// Design parameters for seismic cases
    /// </summary>
    public class Table4_2_SeismicEntry
    {
        /// <summary>
        /// Nail spacing at diaphragm boundaries (all cases)
        /// At continuous panel edges parallel to load (Cases 3 & 4)
        /// At all panel edges (Cases 5 & 6)
        /// </summary>
        public double NailSpacing_Boundary { get; set; }

        /// <summary>
        /// Nail spacing at other panel edges (Cases 1,2,3 & 4)
        /// </summary>
        public double NailSpacing_OtherPanelEdges { get; set; }

        public double v_s { get; set; }  // shear per foot
        public double G_a_OSB { get; set; } // G_a for OSB
        public double G_a_PLY { get; set; } // G_a for PLY
    }

    /// <summary>
    /// Design parameters for wind cases
    /// </summary>
    public class Table4_2_WindEntry
    {
        /// <summary>
        /// Nail spacing at diaphragm boundaries (all cases)
        /// At continuous panel edges parallel to load (Cases 3 & 4)
        /// At all panel edges (Cases 5 & 6)
        /// </summary>
        public double NailSpacing_Boundary { get; set; }

        /// <summary>
        /// Nail spacing at other panel edges (Cases 1,2,3 & 4)
        /// </summary>
        public double NailSpacing_OtherPanelEdges { get; set; }

        public double v_s { get; set; }  // shear per foot
    }

   
    public class Table4_2NominalUnitShearCapacities
    {
        public Table4_2_SeismicEntry Seismic6_6;  // Case A Seismic -- boundaries spa = 6, other edges = 6
        public Table4_2_SeismicEntry Seismic4_6;  // Case A Seismic -- boundaries spa = 4, other edges = 6
        public Table4_2_SeismicEntry Seismic2_5_4;// Case A Seismic -- boundaries spa = 2.5, other edges = 4
        public Table4_2_SeismicEntry Seismic2_3;  // Case A Seismic -- boundaries spa = 2, other edges = 3

        public Table4_2_WindEntry Wind6_6;  // Case B Wind -- boundaries spa = 6, other edges = 6
        public Table4_2_WindEntry Wind4_6;  // Case B Wind -- boundaries spa = 4, other edges = 6
        public Table4_2_WindEntry Wind2_5_4;// Case B Wind -- boundaries spa = 2.5, other edges = 4
        public Table4_2_WindEntry Wind2_3;  // Case B Wind -- boundaries spa = 2, other edges = 3


        public SheathingGrades SheathingGrade { get; set; }
        public CommonNailSizes CommonNailSize { get; set; }
        public double MinimumFastenerPenetration { get; set; }   // min penetration into framing member or blocking
        public NominalPanelThicknesses NominalPanelThickness { get; set; }
        public double NominalWidthOfNailedFaceAtBoundaries { get; set; }
        public double NominalUnitShearCapacity { get; set; }
    }

    public class Table4_2Manager
    {
        private int _current_id = 0;
        public Dictionary<int, Table4_2NominalUnitShearCapacities> Table4_2NominalUnitShearCapacities = new Dictionary<int, Table4_2NominalUnitShearCapacities>();

        private int GetNextId()
        {
            _current_id++;
            return _current_id;
        }
        public Table4_2Manager()
        {
            BuildTable4_2();
        }

        private void BuildTable4_2()
        {
            Table4_2NominalUnitShearCapacities.Clear();
            //Structural 1 entries
            // 6d - 5/16 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GADE_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                   NailSpacing_Boundary = 6,
                   NailSpacing_OtherPanelEdges = 6,
                   v_s = 370,
                   G_a_OSB = 15,
                   G_a_PLY = 12
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 500,
                    G_a_OSB = 8.5,
                    G_a_PLY = 7.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 750,
                    G_a_OSB = 12,
                    G_a_PLY = 10
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 840,
                    G_a_OSB = 20,
                    G_a_PLY = 15
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 520,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 700,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1050,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1175,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GADE_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 420,
                    G_a_OSB = 12,
                    G_a_PLY = 9.5
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 560,
                    G_a_OSB = 7,
                    G_a_PLY = 6
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 840,
                    G_a_OSB = 9.5,
                    G_a_PLY = 8.5
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 950,
                    G_a_OSB = 17,
                    G_a_PLY = 13
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 590,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 785,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1175,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1330,
                },

            });

            // 8d - 3/8 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GADE_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 540,
                    G_a_OSB = 14,
                    G_a_PLY = 11
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 720,
                    G_a_OSB = 9,
                    G_a_PLY = 7.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1060,
                    G_a_OSB = 13,
                    G_a_PLY = 10
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1200,
                    G_a_OSB = 21,
                    G_a_PLY = 15
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 755,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1010,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1485,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1680,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GADE_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 600,
                    G_a_OSB = 12,
                    G_a_PLY = 10
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 800,
                    G_a_OSB = 7.5,
                    G_a_PLY = 6.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1200,
                    G_a_OSB = 10,
                    G_a_PLY = 9
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1350,
                    G_a_OSB = 18,
                    G_a_PLY = 13
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 840,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1120,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1680,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1890,
                },

            });

            // 10d - 15/32 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GADE_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 640,
                    G_a_OSB = 24,
                    G_a_PLY = 17
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 850,
                    G_a_OSB = 15,
                    G_a_PLY = 12
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1280,
                    G_a_OSB = 20,
                    G_a_PLY = 15
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1460,
                    G_a_OSB = 31,
                    G_a_PLY = 21
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 895,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1190,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1790,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2045,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GADE_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 720,
                    G_a_OSB = 20,
                    G_a_PLY = 15
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 960,
                    G_a_OSB = 12,
                    G_a_PLY = 9.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1440,
                    G_a_OSB = 16,
                    G_a_PLY = 13
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1640,
                    G_a_OSB = 26,
                    G_a_PLY = 18
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1010,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1345,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2015,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2295,
                },

            });

            //Sheathing and Single Floor
            // 6d - 5/16 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 340,
                    G_a_OSB = 15,
                    G_a_PLY = 10
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 450,
                    G_a_OSB = 9.0,
                    G_a_PLY = 7.0
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 670,
                    G_a_OSB = 13,
                    G_a_PLY = 9.5
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 760,
                    G_a_OSB = 21,
                    G_a_PLY = 13
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 475,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 630,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 940,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1065,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 380,
                    G_a_OSB = 12,
                    G_a_PLY = 9
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 500,
                    G_a_OSB = 7,
                    G_a_PLY = 6
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 760,
                    G_a_OSB = 10,
                    G_a_PLY = 8
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 860,
                    G_a_OSB = 17,
                    G_a_PLY = 12
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 530,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 700,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1065,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1205,
                },

            });

            // 6d - 3/8 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 370,
                    G_a_OSB = 13,
                    G_a_PLY = 9.5
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 500,
                    G_a_OSB = 7.0,
                    G_a_PLY = 6.0
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 750,
                    G_a_OSB = 10,
                    G_a_PLY = 8
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 840,
                    G_a_OSB = 18,
                    G_a_PLY = 12
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 520,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 700,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1050,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1175,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 420,
                    G_a_OSB = 10,
                    G_a_PLY = 8
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 560,
                    G_a_OSB = 5.5,
                    G_a_PLY = 5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 840,
                    G_a_OSB = 8.5,
                    G_a_PLY = 7
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 950,
                    G_a_OSB = 14,
                    G_a_PLY = 10
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 590,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 785,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1175,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1330,
                },
            });

            // 8d - 3/8 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 480,
                    G_a_OSB = 15,
                    G_a_PLY = 11
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 640,
                    G_a_OSB = 9.5,
                    G_a_PLY = 7.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 960,
                    G_a_OSB = 13,
                    G_a_PLY = 9.5
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1090,
                    G_a_OSB = 21,
                    G_a_PLY = 13
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 670,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 895,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1345,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1525,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 540,
                    G_a_OSB = 12,
                    G_a_PLY = 9.5
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 720,
                    G_a_OSB = 7.5,
                    G_a_PLY = 6.0
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1080,
                    G_a_OSB = 11,
                    G_a_PLY = 8.5
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1220,
                    G_a_OSB = 18,
                    G_a_PLY = 12
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 755,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1010,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1510,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1710,
                },

            });

            // 8d - 7/16 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 510,
                    G_a_OSB = 14,
                    G_a_PLY = 10
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 680,
                    G_a_OSB = 8.5,
                    G_a_PLY = 7.0
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1010,
                    G_a_OSB = 12,
                    G_a_PLY = 9.5
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1150,
                    G_a_OSB = 20,
                    G_a_PLY = 13
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 715,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 950,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1415,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1610,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 570,
                    G_a_OSB = 11,
                    G_a_PLY = 9
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 760,
                    G_a_OSB = 7.0,
                    G_a_PLY = 6.0
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1140,
                    G_a_OSB = 10.0,
                    G_a_PLY = 8.0
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1290,
                    G_a_OSB = 17,
                    G_a_PLY = 12
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 800,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1065,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1595,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1805,
                },

            });

            // 8d - 15/32 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 540,
                    G_a_OSB = 13,
                    G_a_PLY = 9.5
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 720,
                    G_a_OSB = 7.5,
                    G_a_PLY = 6.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1060,
                    G_a_OSB = 11,
                    G_a_PLY = 8.5
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1200,
                    G_a_OSB = 19,
                    G_a_PLY = 13
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 755,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1010,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1485,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1680,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 600,
                    G_a_OSB = 10,
                    G_a_PLY = 8.5
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 800,
                    G_a_OSB = 6.0,
                    G_a_PLY = 5.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1200,
                    G_a_OSB = 9.0,
                    G_a_PLY = 7.5
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1350,
                    G_a_OSB = 15,
                    G_a_PLY = 11
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 840,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1120,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1680,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1890,
                },

            });

            // 10d - 15/32 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 580,
                    G_a_OSB = 25,
                    G_a_PLY = 15
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 770,
                    G_a_OSB = 15,
                    G_a_PLY = 11
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1150,
                    G_a_OSB = 21,
                    G_a_PLY = 14
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1310,
                    G_a_OSB = 33,
                    G_a_PLY = 18
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 810,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1080,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1610,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1835,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 650,
                    G_a_OSB = 21,
                    G_a_PLY = 14
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 860,
                    G_a_OSB = 12,
                    G_a_PLY = 9.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1300,
                    G_a_OSB = 17,
                    G_a_PLY = 12
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1470,
                    G_a_OSB = 28,
                    G_a_PLY = 16
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 910,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1205,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1820,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2060,
                },

            });

            // 10d - 19/32 panel
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 640,
                    G_a_OSB = 21,
                    G_a_PLY = 14
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 850,
                    G_a_OSB = 13,
                    G_a_PLY = 9.5
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1280,
                    G_a_OSB = 18,
                    G_a_PLY = 12
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1460,
                    G_a_OSB = 28,
                    G_a_PLY = 17
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 895,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1190,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1790,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2045,
                },

            });
            Table4_2NominalUnitShearCapacities.Add(GetNextId(), new Table4_2NominalUnitShearCapacities
            {
                SheathingGrade = SheathingGrades.SHEATH_GRADE_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 720,
                    G_a_OSB = 17,
                    G_a_PLY = 12
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 960,
                    G_a_OSB = 10,
                    G_a_PLY = 8
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1440,
                    G_a_OSB = 14,
                    G_a_PLY = 11
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 1640,
                    G_a_OSB = 24,
                    G_a_PLY = 15
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1010,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1345,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2015,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2295,
                },

            });
        }
    }
}
