using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations;
using ShearWallCalculator.WindLoadCalculations.WindLoadCalculators;
using System;
using System.Collections.Generic;
using System.Windows;

namespace WindLoadDataExporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            List<WindLoadCalculator_Base> calculators = new List<WindLoadCalculator_Base>();

            ASCE7_Versions version = ASCE7_Versions.ASCE_VER_7_16;
            WindExposureCategories exposure_cat = WindExposureCategories.WIND_EXP_CAT_C;
            WindLoadCalculationTypes calculation_type = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

            double windSpeed = 150;
            string importanceCategory = "I";
            double Kd = 0.85, Ke = 1.0, Kzt = 1.0;

            int[] heights = { 15, 30, 45 };
            int[] roofPitches = { 15, 30, 45 };

            var buildingSizes = new[]
            {
    new { Length = 60, Width = 40 },
    new { Length = 70, Width = 50 },
    new { Length = 80, Width = 60 }
};

            // Optional: Print CSV header
            Console.WriteLine("RoofType,Length,Width,Height,Pitch");

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

                    WindLoadParameters_Base parameters = WindLoadParametersFactory.Create(
                        roofType, importanceCategory, windSpeed, exposure_cat, Kd, Ke, Kzt, calculation_type);

                    parameters.ComputeEffectiveWindAreas(buildingData, version);

                    WindLoadCalculator_Base calculator = WindLoadCalculatorFactory.Create(
                        version, calculation_type, parameters, buildingData);

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

                            WindLoadParameters_Base parameters = WindLoadParametersFactory.Create(
                                rtype, importanceCategory, windSpeed, exposure_cat, Kd, Ke, Kzt, calculation_type);

                            parameters.ComputeEffectiveWindAreas(buildingData, version);

                            WindLoadCalculator_Base calculator = WindLoadCalculatorFactory.Create(
                                version, calculation_type, parameters, buildingData);

                            calculator.CalculatePressures();
                            calculators.Add(calculator);

                            Console.WriteLine($"{rtype},{size.Length},{size.Width},{height},{pitch}");
                        }
                    }
                }
            }


        }
    }
}
