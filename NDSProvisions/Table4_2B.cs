using System;
using System.Collections.Generic;

namespace ShearWallCalculator.NDSProvisions
{
    /// <summary>
    /// Table 4.2B Nominal Unit Shear Capacities for Wood-Frame Diaphragms
    /// Blocked Wood Structural Panel Diaphragms Utilizing Multiple Rows of Fasteners (High Load Diaphragms)
    /// </summary>
    public class Table4_2B
    {
        public Table4_2_SeismicEntry Seismic6_6;  // Case A Seismic -- boundaries spa = 6, other edges = 6
        public Table4_2_SeismicEntry Seismic4_6;  // Case A Seismic -- boundaries spa = 4, other edges = 6
        public Table4_2_SeismicEntry Seismic2_5_4;// Case A Seismic -- boundaries spa = 2.5, other edges = 4
        public Table4_2_SeismicEntry Seismic2_3;  // Case A Seismic -- boundaries spa = 2, other edges = 3

        public Table4_2_WindEntry Wind6_6;  // Case B Wind -- boundaries spa = 6, other edges = 6
        public Table4_2_WindEntry Wind4_6;  // Case B Wind -- boundaries spa = 4, other edges = 6
        public Table4_2_WindEntry Wind2_5_4;// Case B Wind -- boundaries spa = 2.5, other edges = 4
        public Table4_2_WindEntry Wind2_3;  // Case B Wind -- boundaries spa = 2, other edges = 3

        public SheathingMaterials SheathingGrade { get; set; }
        public CommonNailSizes CommonNailSize { get; set; }
        public double MinimumFastenerPenetration { get; set; }   // min penetration into framing member or blocking
        public NominalPanelThicknesses NominalPanelThickness { get; set; }
        public double NominalWidthOfNailedFaceAtBoundaries { get; set; }
        public double LinesOfFasteners { get; set; }
    }

    public class Table4_2BManager
    {
        public string Title { get; set; } = "Table 4.2B Nominal Unit Shear Capacities for Wood-Frame Diaphragms";
        public string Description { get; set; } = "Blocked Wood Structural Panel Diaphragms Utilizing Multiple Rows of Fasteners (High Load Diaphragms)";

        private int _current_id = 0;
        public Dictionary<int, Table4_2B> Table4_2BNominalUnitShearCapacities = new Dictionary<int, Table4_2B>();

        private int GetNextId()
        {
            _current_id++;
            return _current_id;
        }
        public Table4_2BManager()
        {
            BuildTable4_2B();
        }

        private void BuildTable4_2B()
        {
            Table4_2BNominalUnitShearCapacities.Clear();
            //Structural 1 entries
            // 10d - 15/32
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1210,
                    G_a_OSB = 40,
                    G_a_PLY = 24
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1630,
                    G_a_OSB = 53,
                    G_a_PLY = 28
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1750,
                    G_a_OSB = 50,
                    G_a_PLY = 27
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2300,
                    G_a_OSB = 56,
                    G_a_PLY = 29
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1695,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2280,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2450,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3220,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1400,
                    G_a_OSB = 33,
                    G_a_PLY = 21
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1830,
                    G_a_OSB = 48,
                    G_a_PLY = 27
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2010,
                    G_a_OSB = 44,
                    G_a_PLY = 25
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2580,
                    G_a_OSB = 51,
                    G_a_PLY = 28
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
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 3,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1750,
                    G_a_OSB = 50,
                    G_a_PLY = 27
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2440,
                    G_a_OSB = 61,
                    G_a_PLY = 30
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2570,
                    G_a_OSB = 59,
                    G_a_PLY = 30
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2790,
                    G_a_OSB = 70,
                    G_a_PLY = 32
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2450,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 3415,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3600,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3905,
                },
            });

            // 10d - 19/32 panel
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1340,
                    G_a_OSB = 36,
                    G_a_PLY = 23
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1760,
                    G_a_OSB = 52,
                    G_a_PLY = 29
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1930,
                    G_a_OSB = 47,
                    G_a_PLY = 27
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2510,
                    G_a_OSB = 54,
                    G_a_PLY = 29
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1875,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2465,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2700,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3515,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1560,
                    G_a_OSB = 29,
                    G_a_PLY = 20
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1980,
                    G_a_OSB = 46,
                    G_a_PLY = 27
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2220,
                    G_a_OSB = 40,
                    G_a_PLY = 25
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2880,
                    G_a_OSB = 48,
                    G_a_PLY = 27
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2185,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2770,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3110,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 4030,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 3,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1930,
                    G_a_OSB = 47,
                    G_a_PLY = 27
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2640,
                    G_a_OSB = 60,
                    G_a_PLY = 31
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2810,
                    G_a_OSB = 57,
                    G_a_PLY = 30
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3580,
                    G_a_OSB = 64,
                    G_a_PLY = 32
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2700,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 3695,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3935,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 5010,
                },

            });

            // 10d - 23/32 panel
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_23_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1460,
                    G_a_OSB = 33,
                    G_a_PLY = 22
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1910,
                    G_a_OSB = 50,
                    G_a_PLY = 29
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2100,
                    G_a_OSB = 45,
                    G_a_PLY = 27
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2730,
                    G_a_OSB = 53,
                    G_a_PLY = 30
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2045,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2675,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2940,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3820,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_23_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1710,
                    G_a_OSB = 26,
                    G_a_PLY = 19
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2140,
                    G_a_OSB = 43,
                    G_a_PLY = 27
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2420,
                    G_a_OSB = 37,
                    G_a_PLY = 24
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3130,
                    G_a_OSB = 45,
                    G_a_PLY = 27
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2395,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2995,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3390,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 4380,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_23_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 3,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2100,
                    G_a_OSB = 45,
                    G_a_PLY = 27
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2860,
                    G_a_OSB = 59,
                    G_a_PLY = 32
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3050,
                    G_a_OSB = 56,
                    G_a_PLY = 31
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3600,
                    G_a_OSB = 68,
                    G_a_PLY = 34
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2940,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 4005,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 4270,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 5040,
                },

            });

            //Sheathing and Single Floor entries
            // 10d - 15/32
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1050,
                    G_a_OSB = 43,
                    G_a_PLY = 21
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1450,
                    G_a_OSB = 55,
                    G_a_PLY = 23
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1530,
                    G_a_OSB = 53,
                    G_a_PLY = 23
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2020,
                    G_a_OSB = 58,
                    G_a_PLY = 24
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1470,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2030,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2140,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2830,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1210,
                    G_a_OSB = 36,
                    G_a_PLY = 19
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1630,
                    G_a_OSB = 50,
                    G_a_PLY = 22
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1750,
                    G_a_OSB = 46,
                    G_a_PLY = 21
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2210,
                    G_a_OSB = 55,
                    G_a_PLY = 23
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1695,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2280,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2450,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3095,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 3,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1530,
                    G_a_OSB = 53,
                    G_a_PLY = 23
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2170,
                    G_a_OSB = 62,
                    G_a_PLY = 24
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2260,
                    G_a_OSB = 61,
                    G_a_PLY = 24
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2390,
                    G_a_OSB = 72,
                    G_a_PLY = 26
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2140,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 3040,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3165,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3345,
                },
            });

            // 10d - 19/32
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1300,
                    G_a_OSB = 34,
                    G_a_PLY = 19
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1720,
                    G_a_OSB = 49,
                    G_a_PLY = 23
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 1870,
                    G_a_OSB = 45,
                    G_a_PLY = 22
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2450,
                    G_a_OSB = 52,
                    G_a_PLY = 23
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1820,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2410,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2620,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3430,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1510,
                    G_a_OSB = 27,
                    G_a_PLY = 16
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1930,
                    G_a_OSB = 43,
                    G_a_PLY = 21
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2160,
                    G_a_OSB = 37,
                    G_a_PLY = 20
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2740,
                    G_a_OSB = 46,
                    G_a_PLY = 22
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2115,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2700,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3025,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3835,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 3,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1870,
                    G_a_OSB = 45,
                    G_a_PLY = 22
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2580,
                    G_a_OSB = 57,
                    G_a_PLY = 24
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2730,
                    G_a_OSB = 55,
                    G_a_PLY = 24
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2970,
                    G_a_OSB = 68,
                    G_a_PLY = 26
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2620,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 3610,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3820,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 4160,
                },
            });

            // 10d - 23/32
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_23_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1420,
                    G_a_OSB = 30,
                    G_a_PLY = 18
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1870,
                    G_a_OSB = 46,
                    G_a_PLY = 23
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2040,
                    G_a_OSB = 42,
                    G_a_PLY = 22
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2670,
                    G_a_OSB = 50,
                    G_a_PLY = 24
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1990,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2620,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2855,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3740,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_23_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 2,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 1650,
                    G_a_OSB = 24,
                    G_a_PLY = 16
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2100,
                    G_a_OSB = 40,
                    G_a_PLY = 21
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2350,
                    G_a_OSB = 34,
                    G_a_PLY = 20
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 2890,
                    G_a_OSB = 45,
                    G_a_PLY = 23
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2310,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2940,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 3290,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 4045,
                },

            });
            Table4_2BNominalUnitShearCapacities.Add(GetNextId(), new Table4_2B
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_23_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 4.0,
                LinesOfFasteners = 3,
                Seismic6_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2040,
                    G_a_OSB = 42,
                    G_a_PLY = 22
                },
                Seismic4_6 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2800,
                    G_a_OSB = 56,
                    G_a_PLY = 25
                },
                Seismic2_5_4 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2.4,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 2960,
                    G_a_OSB = 53,
                    G_a_PLY = 25
                },
                Seismic2_3 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 3130,
                    G_a_OSB = 71,
                    G_a_PLY = 28
                },

                Wind6_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 2855,
                },
                Wind4_6 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 4,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 3920,
                },
                Wind2_5_4 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2.5,
                    NailSpacing_OtherPanelEdges = 4,
                    v_s = 4145,
                },
                Wind2_3 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 2,
                    NailSpacing_OtherPanelEdges = 3,
                    v_s = 4380,
                },
            });
        }
    }
}
