using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace WindLoadDataExporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int max_bldg_data_col_num = 6; // includes blank column at right
        private int max_area_data_col_num = 4; // includes blank column at right 
        private int max_flat_roof_data = 9; // includes blank column at right
        private int max_gable_roof_data = 13; // includes blank column at right
        private int max_hip_roof_data = 9; // includes blank column at right
        private int max_wall_data1_col_num = 5; // includes blank column at right
        private int max_wall_data2_col_num = 5; // includes blank column at right



        class BuildingSize
        {
            public double Length { get; set; }
            public double Width { get; set; }
        }

        List<WindLoadCalculator_Base> calculators = new List<WindLoadCalculator_Base>();

        ASCE7_Versions version = ASCE7_Versions.ASCE_VER_7_22;
        WindExposureCategories exposure_cat = WindExposureCategories.WIND_EXP_CAT_C;
        WindLoadCalculationTypes calculation_type = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        double windSpeed = 150;
        string importanceCategory = "I";
        double Kd = 0.85, Ke = 1.0, Kzt = 1.0;

        int[] heights = { 15, 30, 45 };
        int[] roofPitches = { 15, 30, 45 };

        BuildingSize[] buildingSizes = new BuildingSize[]
        {
                new BuildingSize{ Length = 60, Width = 40 },
                new BuildingSize{ Length = 70, Width = 50 },
                new BuildingSize{ Length = 80, Width = 60 }
            };
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            // Define max column counts per group
            var maxGroupSizes = new Dictionary<string, int>
            {
                { "BldgData", max_bldg_data_col_num },
                { "PrelimCalcs", max_area_data_col_num },
                { "Wall_Length", max_wall_data1_col_num },
                { "Wall_Width", max_wall_data2_col_num },
                { "FlatRoof", max_flat_roof_data },
                { "GableRoof", max_gable_roof_data },
                { "HipRoof", max_hip_roof_data }
            };

            var dataRows = new List<Dictionary<string, List<string>>>
            {
                //new Dictionary<string, List<string>>
                //{
                //    { "BldgData", new List<string>{ "A1", "A2" } },        
                //    { "PrelimCalcs", new List<string>{ "B1", "B2", "B3" } },  
                //    { "Wall1", new List<string>{ "C1", "C2", "C3" } },        
                //    { "Wall2", new List<string>{ "D1", "D2", "D3" } },
                //    { "FlatRoof", new List<string>{ "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9" } },
                //    { "HipRoof", new List<string>{ "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9" } },
                //    { "GableRoof", new List<string>{ "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "G10", "G11", "G12", "G13" } }
                //},
                //new Dictionary<string, List<string>>
                //{
                //    { "BldgData", new List<string>{ "A1", "A2", "A3", "A4" } },
                //    { "PrelimCalcs", new List<string>{ "B1", "B2" } },
                //    { "Wall1", new List<string>{ "C1", "C2" } },
                //    { "Wall2", new List<string>{ "D1", "D2", } },
                //    { "FlatRoof", new List<string>{ "E1", "E4", "E5", "E6", "E7", "E8", "E9" } },
                //    { "HipRoof", new List<string>{ "F1", "F5", "F6", "F7", "F8", "F9" } },
                //    { "GableRoof", new List<string>{ "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "G10", "G11", "G12", "G13" } }
                //}
            };




            RoofTypes roofType = RoofTypes.ROOF_TYPE_FLAT;
            int flatPitch = 0;

            foreach (var size in buildingSizes)
            {
                foreach (int height in heights)
                {
                    BuildingData buildingData = new BuildingData()
                    {
                        BuildingHeight = height,
                        BuildingLength = size.Length,
                        BuildingWidth = size.Width,
                        RoofType = roofType,
                        RoofPitch = flatPitch,
                        EnclosureType = BuildingEnclosures.BLDG_ENCLOSED
                    };

                    WindParameters_Base parameters = WindLoadParametersFactory.Create(
                        roofType, importanceCategory, windSpeed, exposure_cat, Kd, Ke, Kzt, calculation_type);



                    WindLoadCalculator_Base calculator = WindLoadCalculatorFactory.Create(
                        version, calculation_type, parameters, buildingData);
                    calculator.CreateAreaCalculators();

                    calculator.CalculatePressures();
                    calculators.Add(calculator);

                    Console.WriteLine($"{roofType},{size.Length},{size.Width},{height},{flatPitch}");
                }
            }

            RoofTypes[] slopedRoofTypes = { RoofTypes.ROOF_TYPE_GABLE, RoofTypes.ROOF_TYPE_HIP };

            foreach (RoofTypes rtype in slopedRoofTypes)
            {
                foreach (var size in buildingSizes)
                {
                    foreach (int height in heights)
                    {
                        foreach (int pitch in roofPitches)
                        {
                            BuildingData buildingData = new BuildingData()
                            {
                                BuildingHeight = height,
                                BuildingLength = size.Length,
                                BuildingWidth = size.Width,
                                RoofType = rtype,
                                RoofPitch = pitch,
                                EnclosureType = BuildingEnclosures.BLDG_ENCLOSED
                            };

                            WindParameters_Base parameters = WindLoadParametersFactory.Create(
                                rtype, importanceCategory, windSpeed, exposure_cat, Kd, Ke, Kzt, calculation_type);

                            WindLoadCalculator_Base calculator = WindLoadCalculatorFactory.Create(
                                version, calculation_type, parameters, buildingData);

                            calculator.CalculatePressures();
                            calculator.CreateAreaCalculators();

                            calculators.Add(calculator);
                        }
                    }
                }
            }

            foreach (var calculator in calculators)
            {
                string roof_type_str = String.Empty;
                switch (calculator.buildingData.RoofType)
                {
                    case RoofTypes.ROOF_TYPE_GABLE:
                        roof_type_str += "GABLE";
                        break;
                    case RoofTypes.ROOF_TYPE_HIP:
                        roof_type_str += "HIP";
                        break;
                    case RoofTypes.ROOF_TYPE_FLAT:
                        roof_type_str += "FLAT";
                        break;
                }

                List<string> bldg_data = new List<string>()
                {
                    roof_type_str,
                    calculator.buildingData.BuildingWidth.ToString(),
                    calculator.buildingData.BuildingLength.ToString(),
                    calculator.buildingData.BuildingHeight.ToString(), 
                    calculator.buildingData.RoofPitch.ToString()
                };


                var h = calculator.buildingData.MeanRoofHeight;
                var a = calculator.RoofAreaCalculator.CritDim_a;
                var qh = calculator.CalculateDynamicWindPressure(h);
                List<string> prelim_calc_data = new List<string>()
                {
                    h.ToString("F2"),
                    qh.ToString("F1"),
                    a.ToString("F1")
                };

                // wall pressures for the BuildingLength wall
                List<string> wall_length_data = GetWallZoneHeaders_BuildingLength(calculator);
                List<string> wall_length_pressures = new List<string>();
                double wall_length_pressure;
                foreach (var data in wall_length_data)
                {
                    // find most positive
                    calculator.TryGetPressureNet_MostPositiveByAreaName_BuildingLengthWall(data, out wall_length_pressure);
                    wall_length_pressures.Add(wall_length_pressure.ToString("F1"));

                    // find most negative
                    calculator.TryGetPressureNet_MostNegativeByAreaName_BuildingLengthWall(data, out wall_length_pressure);
                    wall_length_pressures.Add(wall_length_pressure.ToString("F1"));
                }

                // wall pressures for the BuildingWidth wall
                List<string> wall_width_data = GetWallZoneHeaders_BuildingWidth(calculator);
                List<string> wall_width_pressures = new List<string>();
                double wall_width_pressure;
                foreach (var data in wall_width_data)
                {
                    // find most positive
                    calculator.TryGetPressureNet_MostPositiveByAreaName_BuildingWidthWall(data, out wall_width_pressure);
                    wall_width_pressures.Add(wall_width_pressure.ToString("F1"));

                    // find most negative
                    calculator.TryGetPressureNet_MostNegativeByAreaName_BuildingWidthWall(data, out wall_width_pressure);
                    wall_width_pressures.Add(wall_width_pressure.ToString("F1"));
                }



                List<string> flat_roof_data= new List<string>();
                List<string> flat_roof_pressures = new List<string>();
                double flat_roof_pressure;
                if (calculator.buildingData.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                {
                    flat_roof_data = GetFlatRoofZoneHeaders(calculator);
                    foreach (var data in flat_roof_data)
                    {
                        // find most positive
                        calculator.TryGetPressureNet_MostPositiveByAreaName_Roof(data, out flat_roof_pressure);
                        flat_roof_pressures.Add(flat_roof_pressure.ToString("F1"));

                        // find most negative
                        calculator.TryGetPressureNet_MostNegativeByAreaName_Roof(data, out flat_roof_pressure);
                        flat_roof_pressures.Add(flat_roof_pressure.ToString("F1"));
                    }
                }
                
                List<string> hip_roof_data = new List<string>();
                List<string> hip_roof_pressures = new List<string>();
                double hip_roof_pressure;
                if (calculator.buildingData.RoofType == RoofTypes.ROOF_TYPE_HIP)
                {
                    hip_roof_data = GetHipRoofZoneHeaders(calculator);
                    foreach (var data in hip_roof_data)
                    {
                        // find most positive
                        calculator.TryGetPressureNet_MostPositiveByAreaName_Roof(data, out hip_roof_pressure);
                        hip_roof_pressures.Add(hip_roof_pressure.ToString("F1"));

                        // find most negative
                        calculator.TryGetPressureNet_MostNegativeByAreaName_Roof(data, out hip_roof_pressure);
                        hip_roof_pressures.Add(hip_roof_pressure.ToString("F1"));
                    }
                }

                List<string> gable_roof_data = new List<string>();
                List<string> gable_roof_pressures = new List<string>();
                double gable_roof_pressure;
                if (calculator.buildingData.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                {
                    gable_roof_data = GetGableRoofZoneHeaders(calculator);
                    foreach (var data in gable_roof_data)
                    {
                        // find most positive
                        calculator.TryGetPressureNet_MostPositiveByAreaName_Roof(data, out gable_roof_pressure);
                        gable_roof_pressures.Add(gable_roof_pressure.ToString("F1"));

                        // find most negative
                        calculator.TryGetPressureNet_MostNegativeByAreaName_Roof(data, out gable_roof_pressure);
                        gable_roof_pressures.Add(gable_roof_pressure.ToString("F1"));
                    }
                }

                dataRows.Add(new Dictionary<string, List<string>>
                {
                    { "BldgData", bldg_data },
                    { "PrelimCalcs", prelim_calc_data},
                    { "Wall_Width", wall_width_pressures},
                    { "Wall_Length", wall_length_pressures},
                    { "FlatRoof", flat_roof_pressures },
                    { "GableRoof", gable_roof_pressures},
                    { "HipRoof", hip_roof_pressures }
                });

                //// Build header
                //StringBuilder header = new StringBuilder();
                //foreach (var group in maxGroupSizes)
                //{
                //    for (int i = 1; i <= group.Value; i++)
                //        header.Append($"{group.Key}_Col{i},");
                //}

                //Console.WriteLine(header.ToString().TrimEnd(','));

                // Build rows, adding necessary blanks
                // Open file for writing (overwrite if it exists)
                using (StreamWriter writer = new StreamWriter("results.csv"))
                {
                    foreach (var row in dataRows)
                    {
                        StringBuilder line = new StringBuilder();

                        foreach (var group in maxGroupSizes)
                        {
                            var groupName = group.Key;
                            int maxCols = group.Value;

                            List<string> values = row.ContainsKey(groupName) ? row[groupName] : new List<string>();

                            for (int i = 0; i < maxCols; i++)
                            {
                                string val = i < values.Count ? values[i] : "";
                                line.Append($"{val},");
                            }
                        }

                        Console.WriteLine(line.ToString().TrimEnd(','));

                        writer.WriteLine(line.ToString().TrimEnd(','));
                    }
                }
            }
        }

        private List<string> GetHipRoofZoneHeaders(WindLoadCalculator_Base calculator)
        {
            // get a list of all the area zone names
            List<string> list = new List<string>();

            foreach (var area in calculator.RoofAreaCalculator.effWindAreas.Values)
            {
                string label = area.Label_Full;
                if (!list.Contains(label))
                {
                    // Find insert position using binary search for efficiency
                    int index = list.BinarySearch(label, Comparer<string>.Create((a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a)));
                    if (index < 0) index = ~index; // bitwise complement to get insert index
                    list.Insert(index, label);
                }
            }

            return list;
        }

        private List<string> GetFlatRoofZoneHeaders(WindLoadCalculator_Base calculator)
        {
            // get a list of all the area zone names
            List<string> list = new List<string>();

            foreach (var area in calculator.RoofAreaCalculator.effWindAreas.Values)
            {
                string label = area.Label_Full;
                if (!list.Contains(label))
                {
                    // Find insert position using binary search for efficiency
                    int index = list.BinarySearch(label, Comparer<string>.Create((a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a)));
                    if (index < 0) index = ~index; // bitwise complement to get insert index
                    list.Insert(index, label);
                }
            }

            return list;
        }

        private List<string> GetWallZoneHeaders_BuildingLength(WindLoadCalculator_Base calculator)
        {
            // get a list of all the area zone names
            List<string> list = new List<string>();

            foreach (var area in calculator.WallAreaCalculator_BldgLength.effWindAreas.Values)
            {
                string label = area.Label_Full;
                if (!list.Contains(label))
                {
                    // Find insert position using binary search for efficiency
                    int index = list.BinarySearch(label, Comparer<string>.Create((a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a)));
                    if (index < 0) index = ~index; // bitwise complement to get insert index
                    list.Insert(index, label);
                }
            }

            return list;    
        }

        private List<string> GetWallZoneHeaders_BuildingWidth(WindLoadCalculator_Base calculator)
        {
            // get a list of all the area zone names
            List<string> list = new List<string>();

            foreach (var area in calculator.WallAreaCalculator_BldgWidth.effWindAreas.Values)
            {
                string label = area.Label_Full;
                if (!list.Contains(label))
                {
                    // Find insert position using binary search for efficiency
                    int index = list.BinarySearch(label, Comparer<string>.Create((a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a)));
                    if (index < 0) index = ~index; // bitwise complement to get insert index
                    list.Insert(index, label);
                }
            }

            return list;
        }

        private List<string> GetGableRoofZoneHeaders(WindLoadCalculator_Base calculator)
        {
            // get a list of all the area zone names
            List<string> list = new List<string>();

            foreach (var area in calculator.RoofAreaCalculator.effWindAreas.Values)
            {
                string label = area.Label_Full;
                if (!list.Contains(label))
                {
                    // Find insert position using binary search for efficiency
                    int index = list.BinarySearch(label, Comparer<string>.Create((a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a)));
                    if (index < 0) index = ~index; // bitwise complement to get insert index
                    list.Insert(index, label);
                }
            }

            return list;
        }

        private string GetHipRoofPressures(ASCE7_Versions version, WindLoadCalculator_Base calculator)
        {
            string str = String.Empty;
            if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                str += GetHipRoofPressures_ASCE7_16(calculator);
            }
            else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                str += GetHipPressures_ASCE7_22(calculator);
            }

            return str;
        }

        private string GetHipRoofPressures_ASCE7_16(WindLoadCalculator_Base calculator)
        {
            string str = String.Empty;
            // get a list of all the area zone names
            List<string> list = new List<string>();

            foreach (var area in calculator.RoofAreaCalculator.effWindAreas.Values)
            {
                string label = area.Label_Full;
                if (!list.Contains(label))
                {
                    // Find insert position using binary search for efficiency
                    int index = list.BinarySearch(label, Comparer<string>.Create((a, b) => StringComparer.OrdinalIgnoreCase.Compare(b, a)));
                    if (index < 0) index = ~index; // bitwise complement to get insert index
                    list.Insert(index, label);
                }
            }

            // now we have a list of all the zones so lets print them
            foreach (string areaName in list)
            {
                str += $"{areaName}+,{areaName}-,";
            }
            str+= ","; // add a blank column

            return str;
        }

        private string GetHipPressures_ASCE7_22(WindLoadCalculator_Base calculator)
        {
            throw new NotImplementedException();
        }

        private string GetGableRoofPressures(ASCE7_Versions version, WindLoadCalculator_Base calculator)
        {
            string str = String.Empty;
            if(version == ASCE7_Versions.ASCE_VER_7_16)
            {
                str += GetGableRoofPressures_ASCE7_16(calculator);
            } else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                str += GetGablePressures_ASCE7_22(calculator);
            }

            return str;
        }

        private string GetGablePressures_ASCE7_22(WindLoadCalculator_Base calculator)
        {
            throw new NotImplementedException();
        }

        private string GetGableRoofPressures_ASCE7_16(WindLoadCalculator_Base calculator)
        {
            string str = String.Empty;
            // get a list of all the area zone names
            List<string> list = new List<string>();
            foreach (var area in calculator.RoofAreaCalculator.effWindAreas.Values)
            {
                string label = area.Label_Full;
                if (!list.Contains(label))
                {
                    // Find insert position using binary search for efficiency
                    int index = list.BinarySearch(label, StringComparer.OrdinalIgnoreCase);
                    if (index < 0) index = ~index; // bitwise complement to get insert index
                    list.Insert(index, label);
                }
            }

            // now we have a list of all the zones so lets print them
            foreach (string areaName in list)
            {
                str += $"{areaName}+,{areaName}-,";
            }

            return str;
        }
    }
}
