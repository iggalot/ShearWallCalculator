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
                results.Add(new WindPressureResult
                {
                    Surface = kvp.Key,
                    Pressure = Math.Round(kvp.Value, 2)
                });
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
        public double Pressure { get; set; }
    }

    // Result class for roof zone pressure calculations
    public class RoofZonePressure
    {
        public string ZoneName { get; set; }
        public double StartDistance { get; set; }
        public double EndDistance { get; set; }
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
            double Kz = GetKzApprox(z, p.ExposureCategory);
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
                    Pressure = Math.Round(qz * -0.9, 2) // Cp for windward roof
                });

                // Leeward Roof (single pressure)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Leeward Roof",
                    StartDistance = 0,
                    EndDistance = L,
                    Pressure = Math.Round(qz * 0.3, 2) // Cp for leeward roof
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
                    Pressure = Math.Round(qz * -1.3, 2) // Cp for edge zone
                });

                // Zone 2: From 'a' to '2a' (Transition zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 2 (Transition)",
                    StartDistance = a,
                    EndDistance = 2 * a,
                    Pressure = Math.Round(qz * -1.0, 2) // Cp for transition zone
                });

                // Zone 1: From '2a' to end of building (Interior zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 1 (LW Edge)",
                    StartDistance = 2 * a,
                    EndDistance = L,
                    Pressure = Math.Round(qz * -0.9, 2) // Cp for interior zone
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
            double Kz = GetKzApprox(z, p.ExposureCategory);
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
        private static double GetKzApprox(double h, string exposure)
        {
            if (exposure == "B") return h <= 30 ? 0.62 : 0.70;
            if (exposure == "C") return h <= 30 ? 0.76 : 0.85;
            if (exposure == "D") return h <= 30 ? 0.91 : 1.0;
            return 0.85;
        }

        // Cp values for different roof zones
        private static double GetCpLeeward(WindLoadParameters p) => -0.3;
        private static double GetCpSidewall(WindLoadParameters p) => -0.7;
        private static double GetCpRoofWindward(WindLoadParameters p) => -0.9;
        private static double GetCpRoofLeeward(WindLoadParameters p) => -0.3;
    }
}
