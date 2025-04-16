using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadInputControl : UserControl
    {
        public WindLoadInputControl()
        {
            InitializeComponent();
        }

        // Event handler for the Compute Button click
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            WindLoadParameters parameters = GetWindLoadParameters();
            Dictionary<string, double> pressures = WindLoadCalculator.CalculateMWFRS(parameters);

            // Display results in the DataGrid
            List<WindPressureResult> results = new List<WindPressureResult>();
            foreach (var kvp in pressures)
            {
                WindPressureResult wpr = new WindPressureResult();
                wpr.Surface = kvp.Key;
                wpr.Pressure = Math.Round(kvp.Value, 2);

                if (kvp.Key == "Windward Wall")
                {
                    wpr.Cp = 0.8;
                }
                else if (kvp.Key == "Leeward Wall")
                {
                    wpr.Cp = WindLoadCalculator.GetCpLeeward(parameters);
                }
                else if (kvp.Key == "Sidewall")
                {
                    wpr.Cp = WindLoadCalculator.GetCpSidewall(parameters); ;
                }

                results.Add(wpr);
            }

            tbl_qh.Text = Math.Round(WindLoadCalculator.CalculateWindPressure(parameters), 2).ToString();

            ResultsDataGrid.ItemsSource = null;
            ResultsDataGrid.ItemsSource = results;

            // Calculate and display roof zone pressures
            List<RoofZonePressure> roofZonePressures = WindLoadCalculator.CalculateRoofZonePressures(parameters);
            RoofZoneResultsDataGrid.ItemsSource = null;
            RoofZoneResultsDataGrid.ItemsSource = roofZonePressures;
        }

        // Method to retrieve parameters from the input fields
        private WindLoadParameters GetWindLoadParameters()
        {
            double windSpeed = double.Parse(WindSpeedTextBox.Text);
            double buildingHeight = double.Parse(BuildingHeightTextBox.Text);
            double kd = double.Parse(KdTextBox.Text);
            double kzt = double.Parse(KztTextBox.Text);
            double importance = double.Parse(ImportanceFactorTextBox.Text);
            double length = double.Parse(BuildingLengthTextBox.Text);
            double width = double.Parse(BuildingWidthTextBox.Text);
            double pitch = double.Parse(RoofPitchTextBox.Text);

            string risk = ((ComboBoxItem)RiskCategoryComboBox.SelectedItem).Content.ToString();
            string exposure = ((ComboBoxItem)ExposureCategoryComboBox.SelectedItem).Content.ToString();
            string enclosure = ((ComboBoxItem)EnclosureComboBox.SelectedItem).Content.ToString();
            string ridgeDir = ((ComboBoxItem)RidgeDirectionComboBox.SelectedItem).Content.ToString();

            return new WindLoadParameters
            {
                RiskCategory = risk,
                WindSpeed = windSpeed,
                ExposureCategory = exposure,
                BuildingHeight = buildingHeight,
                EnclosureClassification = enclosure,
                Kd = kd,
                Kzt = kzt,
                ImportanceFactor = importance,
                BuildingLength = length,
                BuildingWidth = width,
                RoofPitch = pitch,
                RidgeDirection = ridgeDir
            };
        }
    }

    // Wind Load Parameters class
    public class WindLoadParameters
    {
        public string RiskCategory { get; set; }
        public double WindSpeed { get; set; }
        public string ExposureCategory { get; set; }
        public double BuildingHeight { get; set; }
        public string EnclosureClassification { get; set; }
        public double Kd { get; set; }
        public double Kzt { get; set; }
        public double ImportanceFactor { get; set; }
        public double BuildingLength { get; set; }
        public double BuildingWidth { get; set; }
        public double RoofPitch { get; set; }
        public string RidgeDirection { get; set; }
    }

    // Result class for wind pressure calculations
    public class WindPressureResult
    {
        public string Surface { get; set; }
        public double Cp { get; set; }

        public double Pressure { get; set; }
    }

    // Result class for roof zone pressure calculations
    public class RoofZonePressure
    {
        public string ZoneName { get; set; }
        public double StartDistance { get; set; }
        public double EndDistance { get; set; }
        public double Cp { get; set; }
        public double Pressure { get; set; }
    }

    // Wind Load Calculator class
    public static class WindLoadCalculator
    {
        public static Dictionary<string, double> CalculateMWFRS(WindLoadParameters p)
        {
            double V = p.WindSpeed;
            double z = p.BuildingHeight;
            double G = 0.85;
            double Kd = p.Kd;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * Kd * V * V * I;
            double GCpi = GetGCpi(p.EnclosureClassification);

            double CpWindward = 0.8; // constant for windward
            double CpLeeward = GetCpLeeward(p);
            double CpSide = GetCpSidewall(p);
            double CpRoofWindward = GetCpRoofWindward(p);
            double CpRoofLeeward = GetCpRoofLeeward(p);

            return new Dictionary<string, double>
            {
                ["Windward Wall"] = qz * G * CpWindward - qz * GCpi,
                ["Leeward Wall"] = qz * G * CpLeeward - qz * GCpi,
                ["Sidewall"] = qz * G * CpSide - qz * GCpi,
            };
        }

        // Calculate roof zone pressures based on distances and Cp values
        public static List<RoofZonePressure> CalculateRoofZonePressures(WindLoadParameters p)
        {
            List<RoofZonePressure> zones = new List<RoofZonePressure>();

            double meanRoofHeight = p.BuildingHeight / 2.0; // Simplified for this example
            double L = p.BuildingLength;
            double qz = CalculateWindPressure(p);

            // Calculate 'a' for flat roofs or parallel ridge roofs per ASCE 7-16
            double a = Math.Min(0.1 * L, 0.4 * meanRoofHeight);

            // Perpendicular to Wind (Windward and Leeward zones)
            if (p.RidgeDirection == "Perpendicular to Wind")
            {
                // Windward Roof (single pressure)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Windward Roof",
                    StartDistance = 0,
                    EndDistance = L,
                    Pressure = Math.Round(qz * -0.9, 2), // Cp for windward roof
                    Cp = GetCpRoofWindward(p)
                });

                // Leeward Roof (single pressure)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Leeward Roof",
                    StartDistance = 0,
                    EndDistance = L,
                    Pressure = Math.Round(qz * 0.3, 2), // Cp for leeward roof
                    Cp = GetCpRoofLeeward(p)

                });
            }
            // Parallel to Wind (Multiple zones like flat roof)
            else if (p.RidgeDirection == "Parallel to Wind")
            {
                // Zone 3: From 0 to 'a' (Edge zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 3 (WW Edge)",
                    StartDistance = 0,
                    EndDistance = a,
                    Pressure = Math.Round(qz * -1.3, 2), // Cp for edge zone
                    Cp = 0.0
                });

                // Zone 2: From 'a' to '2a' (Transition zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 2 (Transition)",
                    StartDistance = a,
                    EndDistance = 2 * a,
                    Pressure = Math.Round(qz * -1.0, 2), // Cp for transition zone
                    Cp = 0.0
                });

                // Zone 1: From '2a' to end of building (Interior zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 1 (LW Edge)",
                    StartDistance = 2 * a,
                    EndDistance = L,
                    Pressure = Math.Round(qz * -0.9, 2), // Cp for interior zone
                    Cp = 0.0
                });
            }

            return zones;
        }

        // Calculate wind pressure based on wind speed and other parameters
        public static double CalculateWindPressure(WindLoadParameters p)
        {
            double V = p.WindSpeed;
            double z = p.BuildingHeight;
            double Kd = p.Kd;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qh = 0.00256 * Kz * Kzt * Kd * V * V * I;
            return qh;
        }

        // Get GCpi based on enclosure classification
        private static double GetGCpi(string enclosure)
        {
            if (enclosure == "Enclosed") return 0.18;
            if (enclosure == "Partially Enclosed") return 0.55;
            if (enclosure == "Open") return 0.30;
            return 0.18;
        }

        // Get Kz approximation based on building height and exposure category
        private static double GetKz(double z, string exposure)
        {
            double zg, alpha;

            switch (exposure)
            {
                case "B":
                    zg = 1200;
                    alpha = 7.0;
                    break;
                case "C":
                    zg = 900;
                    alpha = 9.5;
                    break;
                case "D":
                    zg = 700;
                    alpha = 11.5;
                    break;
                default:
                    zg = 900;
                    alpha = 9.5;
                    break;
            }

            z = Math.Max(z, 15); // Minimum height for Kz is 15 ft
            return 2.01 * Math.Pow(z / zg, 2.0 / alpha);
        }

        // Cp values for different roof zones and surfaces
        public static double GetCpLeeward(WindLoadParameters p)
        {
            double length = p.BuildingLength;
            double width = p.BuildingWidth;

            // Ensure ratio is length over width (L/B) ≥ 1
            double ratio = length >= width ? length / width : width / length;

            // Approximate Cp values based on ASCE 7-16 Figure 27.4-1
            if (ratio <= 1.0)
                return -0.3;
            else if (ratio <= 2.0)
                return -0.5;
            else
                return -0.6;
        }
        public static double GetCpSidewall(WindLoadParameters p) => -0.7;
        public static double GetCpRoofWindward(WindLoadParameters p)
        {
            double pitch = p.RoofPitch;
            string ridgeDir = p.RidgeDirection;

            if (pitch <= 10.0) // Low slope, treated like flat
                return -0.9;

            if (ridgeDir == "Perpendicular to Wind")
            {
                if (pitch <= 20.0) return -0.9;
                return -1.1;
            }
            else // Parallel to Wind
            {
                // Use zone pressures based on ASCE 7-16 for flat roof approximation
                return -0.5;
            }
        }
        public static double GetCpRoofLeeward(WindLoadParameters p)
        {
            double pitch = p.RoofPitch;
            string ridgeDir = p.RidgeDirection;

            if (pitch <= 10.0 || ridgeDir == "Parallel to Wind")
            {
                // Flat roof or parallel ridge — zoned analysis should be used
                return -0.3; // base value, but actual implementation should use zone Cp
            }
            else if (ridgeDir == "Perpendicular to Wind")
            {
                if (pitch <= 20.0) return -0.3;
                return -0.5; // steeper roofs have more suction on leeward face
            }

            return -0.3; // fallback
        }
    }
}
