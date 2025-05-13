using System.Collections.Generic;

namespace ShearWallCalculator.NDSProvisions
{
    /// Design parameters for seismic cases
    /// </summary>
    public class Table4_3C_SeismicEntry
    {
        public double v_s { get; set; }  // shear per foot
        public double G_a_OSB { get; set; } // G_a for OSB
        public double G_a_PLY { get; set; } // G_a for PLY
    }

    /// <summary>
    /// Design parameters for wind cases
    /// </summary>
    public class Table4_3C_WindEntry
    {
        public double v_s { get; set; }  // shear per foot
    }
   
    /// <summary>
    /// Table 4.3C Nominal Unit Shear Capacities for Wood-Frame Shear Walls
    /// Gypsum and Portland Cement Plaster
    /// </summary>
    public class Table4_3C
    {
        public Table4_3C_SeismicEntry Seismic; // Case A Seismic,

        public Table4_3C_WindEntry Wind; // Case B Wind

        public SheathingMaterials SheathingType { get; set; }
        public NominalPanelThicknesses NominalPanelThickness { get; set; }
        public string NominalPanelThicknessNote { get; set; } = string.Empty;
        public string MaxFastenerEdgeSpacing { get; set; }
        public double MaxStudSpacing { get; set; } 
        public string FastenerTypeAndSize1 { get; set; }
        public string FastenerTypeAndSize2 { get; set; }
        public string FastenerTypeAndSize3 { get; set; }
        public string FastenerTypeAndSize4 { get; set; } = string.Empty;  // used in 5/8" two-ply installs
        public string FastenerTypeAndSize5 { get; set; } = string.Empty;  // used in 5/8" two-ply installs
        public string FastenerTypeAndSize6 { get; set; } = string.Empty;  // used in 5/8" two-ply installs


        public string Note { get; set; }   // for storing additional information
    }

    public class Table4_3CManager
    {
        public string Title { get; set; } = "Table 4.3C Nominal Unit Shear Capacities for Wood-Frame Shear Walls";
        public string Description { get; set; } = "Gypsum and Portland Cement Plaster";

        private int _current_id = 0;
        public Dictionary<int, Table4_3C> Table4_3CNominalUnitShearCapacities = new Dictionary<int, Table4_3C>();

        private int GetNextId()
        {
            _current_id++;
            return _current_id;
        }
        public Table4_3CManager()
        {
            BuildTable4_3C();
        }

        private void BuildTable4_3C()
        {
            Table4_3CNominalUnitShearCapacities.Clear();

            // Gypsum wallboard, gypsum base, veneer platers, or water resistant gpsum backing board
            // 1/2" panel -- 24in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "5d cooler (0.086in. x 1.625in long. 15/64in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.086in. x 1 5-8in long. 9/32 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.5in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "7",
                MaxStudSpacing = 24,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 150,
                    G_a_OSB = 4,
                    G_a_PLY = 4
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 150,
                },
            });
            // 1/2" panel -- 7in. fastener spacing, 24in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "5d cooler (0.086in. x 1.625in long. 15/64in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.086in. x 1 5-8in long. 9/32 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.5in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "7",
                MaxStudSpacing = 24,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 150,
                    G_a_OSB = 4,
                    G_a_PLY = 4
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 150,
                },
            });
            // 1/2" panel -- 4in. fastener spacing, 24in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "5d cooler (0.086in. x 1.625in long. 15/64in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.086in. x 1 5-8in long. 9/32 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.5in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "4",
                MaxStudSpacing = 24,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 220,
                    G_a_OSB = 6,
                    G_a_PLY = 6
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 220,
                },
            });
            // 1/2" panel -- 7in. fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "5d cooler (0.086in. x 1.625in long. 15/64in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.086in. x 1 5-8in long. 9/32 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.5in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "7",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 200,
                    G_a_OSB = 5.5,
                    G_a_PLY = 5.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 200,
                },
            });
            // 1/2" panel -- 4in. fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "5d cooler (0.086in. x 1.625in long. 15/64in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.086in. x 1 5-8in long. 9/32 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.5in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "4",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 250,
                    G_a_OSB = 6.5,
                    G_a_PLY = 6.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 250,
                },
            });
            // 1/2" panel -- 7in. fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "5d cooler (0.086in. x 1.625in long. 15/64in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.086in. x 1 5-8in long. 9/32 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.5in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "7",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 250,
                    G_a_OSB = 6.5,
                    G_a_PLY = 6.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 250,
                },
            });
            // 1/2" panel -- 4in. fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "5d cooler (0.086in. x 1.625in long. 15/64in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.086in. x 1 5-8in long. 9/32 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.5in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "4",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 300,
                    G_a_OSB = 7.5,
                    G_a_PLY = 7.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 300,
                },
            });

            // 1/2" panel -- 8 / 12  fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "No. 6 Type S or W drywall screws 1.25in. long (ASTM C 1002)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "8 / 12",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 120,
                    G_a_OSB = 3.5,
                    G_a_PLY = 3.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 120,
                },
            });
            // 1/2" panel -- 4 / 16 fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "No. 6 Type S or W drywall screws 1.25in. long (ASTM C 1002)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "4 / 16",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 320,
                    G_a_OSB = 8.0,
                    G_a_PLY = 8.0
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 320,
                },
            });
            // 1/2" panel -- 4 / 12 fastener spacing, 24in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "No. 6 Type S or W drywall screws 1.25in. long (ASTM C 1002)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "4 / 12",
                MaxStudSpacing = 24,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 310,
                    G_a_OSB = 8.0,
                    G_a_PLY = 8.0
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 310,
                },
            });
            // 1/2" panel -- 8 / 12 fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "No. 6 Type S or W drywall screws 1.25in. long (ASTM C 1002)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "8 / 12",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 140,
                    G_a_OSB = 4.0,
                    G_a_PLY = 4.0
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 140,
                },
            });
            // 1/2" panel -- 6 / 12 fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                FastenerTypeAndSize1 = "No. 6 Type S or W drywall screws 1.25in. long (ASTM C 1002)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "6 / 12",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 180,
                    G_a_OSB = 5.0,
                    G_a_PLY = 5.0
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 180,
                },
            });

            // 5/8" panel -- 7" edge spa -- 24in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN,
                FastenerTypeAndSize1 = "6d cooler (0.092in. x 1.875in long. 1/4in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.0915in. x 1.875in. long. 19/64 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.75in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "7",
                MaxStudSpacing = 24,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 230,
                    G_a_OSB = 6,
                    G_a_PLY = 6
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 230,
                },
            });
            // 5/8" panel -- 4" edge spa -- 24in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN,
                FastenerTypeAndSize1 = "6d cooler (0.092in. x 1.875in long. 1/4in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.0915in. x 1.875in. long. 19/64 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.75in. long, min. 3/8in. head",
                MaxFastenerEdgeSpacing = "4",
                MaxStudSpacing = 24,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 290,
                    G_a_OSB = 7.5,
                    G_a_PLY = 7.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 290,
                },
            });
            // 5/8" panel -- 7" edge spa -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN,
                FastenerTypeAndSize1 = "6d cooler (0.092in. x 1.875in long. 1/4in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.0915in. x 1.875in. long. 19/64 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.75in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "7",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 290,
                    G_a_OSB = 7.5,
                    G_a_PLY = 7.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 290,
                },
            });
            // 5/8" panel -- 4" edge spa -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN,
                FastenerTypeAndSize1 = "6d cooler (0.092in. x 1.875in long. 1/4in. head)",
                FastenerTypeAndSize2 = "wallboard nailed (0.0915in. x 1.875in. long. 19/64 in. head)",
                FastenerTypeAndSize3 = "0.120in. nail x 1.75in. long, min. 3/8in. head",
                MaxFastenerEdgeSpacing = "4",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 350,
                    G_a_OSB = 8.5,
                    G_a_PLY = 8.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 350,
                },
            });
            // 5/8" panel -- 8 / 12  fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN,
                FastenerTypeAndSize1 = "No. 6 Type S or W drywall screws 1.25in. long (ASTM C 1002)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "8 / 12",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 140,
                    G_a_OSB = 4,
                    G_a_PLY = 4
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 140,
                },
            });
            // 5/8" panel -- 8 / 12  fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN,
                FastenerTypeAndSize1 = "No. 6 Type S or W drywall screws 1.25in. long (ASTM C 1002)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "8 / 12",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 180,
                    G_a_OSB = 5,
                    G_a_PLY = 5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 180,
                },
            });

            // 5/8" panel -- Base: 9  fastener spacing, 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_WALLBOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_5_8IN_2PLY,
                FastenerTypeAndSize1 = "Base Ply: 6d cooler (0.092in x 1.875in.long",
                FastenerTypeAndSize2 = "Base Ply: wallboard nail (0.0915in. x 1.875in. long. 19/64 in. head)",
                FastenerTypeAndSize3 = "Base Ply: 0.120in. nail x 1.75in. long, min. 3/8in. head",
                FastenerTypeAndSize4 = "Face Ply: 8d cooler (0.113in x 2.375in.long. 0.281in. head)",
                FastenerTypeAndSize5 = "Face Ply: wallboard nail (0.113in. x 2.375in. long. 0.375in. head)",
                FastenerTypeAndSize6 = "Face Ply: 0.120in. nail x 2.375in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "Base: 9 -- Face: 7",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 500,
                    G_a_OSB = 11,
                    G_a_PLY = 11
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 500,
                },
            });


            // Gypsum Sheathing Board
            // 1/2"x2'x8' panel -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_SHEATHING_BOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                NominalPanelThicknessNote = "1/2in. x 2ft. x 8ft.",
                FastenerTypeAndSize1 = "0.120in. nail x 1.75in. long, min. 7/16in. head, diamond point, galvanized",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "4",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 150,
                    G_a_OSB = 4,
                    G_a_PLY = 4
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 150,
                },
            });
            // 1/2"x4' panel -- 4" spa -- 24in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_SHEATHING_BOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                NominalPanelThicknessNote = "1/2in. x 4ft.",
                FastenerTypeAndSize1 = "0.120in. nail x 1.75in. long, min. 7/16in. head, diamond point, galvanized",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "4",
                MaxStudSpacing = 24,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 350,
                    G_a_OSB = 8.5,
                    G_a_PLY = 8.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 350,
                },
            });
            // 1/2"x4' panel -- 7" spa -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_SHEATHING_BOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                NominalPanelThicknessNote = "1/2in. x 4ft.",
                FastenerTypeAndSize1 = "0.120in. nail x 1.75in. long, min. 7/16in. head, diamond point, galvanized",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "7",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 200,
                    G_a_OSB = 5.5,
                    G_a_PLY = 5.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 200,
                },
            });
            // 5/8"x4' panel -- 4 / 7 spa -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_SHEATHING_BOARD,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_1_2IN,
                NominalPanelThicknessNote = "5/8in. x 4ft.",
                FastenerTypeAndSize1 = "6d galvanized cooler (0.092in. x 1.875in. long, 1/4in. head)",
                FastenerTypeAndSize2 = "wallboard nail (0.915in. x 1.875in. long, 19/64in. head",
                FastenerTypeAndSize3 = "0.120in. x 1.25in. long, min. 3/8in. head",

                MaxFastenerEdgeSpacing = "4 / 7",
                MaxStudSpacing = 16,
                Note = "blocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 400,
                    G_a_OSB = 9.5,
                    G_a_PLY = 9.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 400,
                },
            });

            // 3/8in. Gypsum Lath and 1/2in. plaster - vertical joints staggered -- 5 spa -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_LATH_PLASTER,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalPanelThicknessNote = "1/2in. plaster and 3/8in. gypsum lath, vertical joints staggered",
                FastenerTypeAndSize1 = "gypsum wallboard blue nail (0.092in. x 1.125in. long, 19/64in. head)",
                FastenerTypeAndSize2 = "0.120in. x 1.75in. long, min. 3/8in. head",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "5",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 360,
                    G_a_OSB = 9.0,
                    G_a_PLY = 9.0
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 360,
                },
            });
            // 3/8in. Gypsum Lath  -- 5 spa -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_GYPSUM_LATH_PLASTER,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_3_8IN,
                NominalPanelThicknessNote = "1/2in. plaster and 3/8in. gypsum lath",

                FastenerTypeAndSize1 = "gypsum wallboard blue nail (0.092in. x 1.125in. long, 19/64in. head)",
                FastenerTypeAndSize2 = "0.120in. x 1.75in. long, min. 3/8in. head",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "5",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 200,
                    G_a_OSB = 5.5,
                    G_a_PLY = 5.5
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 200,
                },
            });

            // 7/8in. Plaster over exposed metal  -- 6 spa -- 16in. stud spacing
            Table4_3CNominalUnitShearCapacities.Add(GetNextId(), new Table4_3C
            {
                SheathingType = SheathingMaterials.SHEATH_MAT_CEMENT_PLASTER_OVER_METAL,
                NominalPanelThickness = NominalPanelThicknesses.THICKNESS_7_8IN,
                NominalPanelThicknessNote = "Expanded metal or woven wire lath and portland cementer plaster",

                FastenerTypeAndSize1 = "gypsum wallboard blue nail (0.092in. x 1.5in. long, 7/16in. head)",
                FastenerTypeAndSize2 = "",
                FastenerTypeAndSize3 = "",

                MaxFastenerEdgeSpacing = "6",
                MaxStudSpacing = 16,
                Note = "unblocked",
                Seismic = new Table4_3C_SeismicEntry
                {
                    v_s = 360,
                    G_a_OSB = 9.0,
                    G_a_PLY = 9.0
                },

                Wind = new Table4_3C_WindEntry
                {
                    v_s = 360,
                },
            });


        }
    }
}
