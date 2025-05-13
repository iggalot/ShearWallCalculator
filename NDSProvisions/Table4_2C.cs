using System.Collections.Generic;

namespace ShearWallCalculator.NDSProvisions
{
    /// <summary>
    /// Table 4.2C Nominal Unit Shear Capacities for Wood-Frame Diaphragms
    /// Unblocked Wood Structural Panel Diaphragms Utilizing
    /// </summary>
    public class Table4_2C
    {
        public Table4_2_SeismicEntry Seismic6_Case1;        // Case A Seismic -- boundaries spa = 6, Case 1 orientation
        public Table4_2_SeismicEntry Seismic6_OtherCases;   // Case A Seismic -- boundaries spa = 6, Case 2-6 orientation

        public Table4_2_WindEntry Wind6_Case1;              // Case B Wind -- boundaries spa = 6, Case 1 Orientation
        public Table4_2_WindEntry Wind6_OtherCases;  // Case B Wind -- boundaries spa = 6, Case 2-6 Orientation

        public SheathingMaterials SheathingGrade { get; set; }
        public CommonNailSizes CommonNailSize { get; set; }
        public double MinimumFastenerPenetration { get; set; }   // min penetration into framing member or blocking
        public NominalPanelThicknesses NominalPanelThickness { get; set; }
        public double NominalWidthOfNailedFaceAtBoundaries { get; set; }
    }

    public class Table4_2CManager
    {
        private int _current_id = 0;
        public Dictionary<int, Table4_2C> Table4_2CNominalUnitShearCapacities = new Dictionary<int, Table4_2C>();

        private int GetNextId()
        {
            _current_id++;
            return _current_id;
        }
        public Table4_2CManager()
        {
            BuildTable4_2C();
        }

        private void BuildTable4_2C()
        {
            Table4_2CNominalUnitShearCapacities.Clear();
            //Structural 1 entries
            // 6d - 5/16
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 330,
                    G_a_OSB = 9.0,
                    G_a_PLY = 7.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 250,
                    G_a_OSB = 6.0,
                    G_a_PLY = 4.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 460,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 350,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 370,
                    G_a_OSB = 7.0,
                    G_a_PLY = 6.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 280,
                    G_a_OSB = 4.5,
                    G_a_PLY = 4.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 520,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 390,
                },
            });

            // 8d - 5/16
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 480,
                    G_a_OSB = 8.5,
                    G_a_PLY = 7.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 360,
                    G_a_OSB = 6.0,
                    G_a_PLY = 4.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 670,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 505,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 530,
                    G_a_OSB = 7.5,
                    G_a_PLY = 6.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 400,
                    G_a_OSB = 5.0,
                    G_a_PLY = 4.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 740,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 560,
                },
            });

            // 10d - 15/32
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 570,
                    G_a_OSB = 14,
                    G_a_PLY = 10
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 430,
                    G_a_OSB = 9.5,
                    G_a_PLY = 7.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 800,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 600,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_STRUCTURAL_1,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 640,
                    G_a_OSB = 12,
                    G_a_PLY = 9.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 480,
                    G_a_OSB = 8.0,
                    G_a_PLY = 6.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 895,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 670,
                },
            });

            //Sheathing and Single Floor entries
            // 6d - 5/16
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 300,
                    G_a_OSB = 9,
                    G_a_PLY = 6.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 220,
                    G_a_OSB = 6.0,
                    G_a_PLY = 4.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 420,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 310,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 340,
                    G_a_OSB = 7,
                    G_a_PLY = 5.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 250,
                    G_a_OSB = 5.0,
                    G_a_PLY = 3.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 475,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 350,
                },
            });

            // 6d - 3/8
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 330,
                    G_a_OSB = 7.5,
                    G_a_PLY = 5.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 250,
                    G_a_OSB = 5.0,
                    G_a_PLY = 4.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 460,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 350,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_6D,
                MinimumFastenerPenetration = 1.25,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 370,
                    G_a_OSB = 6,
                    G_a_PLY = 4.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 280,
                    G_a_OSB = 4.0,
                    G_a_PLY = 3.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 520,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 390,
                },
            });

            // 8d - 3/8
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 430,
                    G_a_OSB = 9.0,
                    G_a_PLY = 6.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 320,
                    G_a_OSB = 6.0,
                    G_a_PLY = 4.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 600,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 450,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 480,
                    G_a_OSB = 7.5,
                    G_a_PLY = 5.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 360,
                    G_a_OSB = 5.0,
                    G_a_PLY = 3.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 670,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 505,
                },
            });

            // 8d - 7/16
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 460,
                    G_a_OSB = 8.5,
                    G_a_PLY = 6.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 340,
                    G_a_OSB = 5.5,
                    G_a_PLY = 4.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 645,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 475,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_16IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 510,
                    G_a_OSB = 7.0,
                    G_a_PLY = 5.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 380,
                    G_a_OSB = 4.5,
                    G_a_PLY = 3.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 715,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 530,
                },
            });

            // 8d - 15/32
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 480,
                    G_a_OSB = 7.5,
                    G_a_PLY = 5.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 360,
                    G_a_OSB = 5.0,
                    G_a_PLY = 4.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 670,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 505,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_8D,
                MinimumFastenerPenetration = 1.375,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 530,
                    G_a_OSB = 6.5,
                    G_a_PLY = 5.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 400,
                    G_a_OSB = 4.0,
                    G_a_PLY = 3.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 740,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 560,
                },
            });

            // 10d - 15/32
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 510,
                    G_a_OSB = 15,
                    G_a_PLY = 9.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 380,
                    G_a_OSB = 10,
                    G_a_PLY = 6.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 715,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 530,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_15_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 580,
                    G_a_OSB = 12,
                    G_a_PLY = 8.0
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 430,
                    G_a_OSB = 8.0,
                    G_a_PLY = 5.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 810,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 600,
                },
            });

            // 10d - 19/32
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 2.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 570,
                    G_a_OSB = 13,
                    G_a_PLY = 8.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 430,
                    G_a_OSB = 8.5,
                    G_a_PLY = 5.5
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 800,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 600,
                },
            });
            Table4_2CNominalUnitShearCapacities.Add(GetNextId(), new Table4_2C
            {
                SheathingGrade = SheathingMaterials.SHEATH_MAT_SHEATHING_AND_SINGLE_FLOOR,
                CommonNailSize = CommonNailSizes.NAILSIZE_10D,
                MinimumFastenerPenetration = 1.5,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_19_32IN,
                NominalWidthOfNailedFaceAtBoundaries = 3.0,
                Seismic6_Case1 = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 640,
                    G_a_OSB = 10,
                    G_a_PLY = 7.5
                },
                Seismic6_OtherCases = new Table4_2_SeismicEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = -1,
                    v_s = 480,
                    G_a_OSB = 7.0,
                    G_a_PLY = 5.0
                },
                Wind6_Case1 = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 895,
                },
                Wind6_OtherCases = new Table4_2_WindEntry
                {
                    NailSpacing_Boundary = 6,
                    NailSpacing_OtherPanelEdges = 6,
                    v_s = 670,
                },
            });
        }
    }
}
