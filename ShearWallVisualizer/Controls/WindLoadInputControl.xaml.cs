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

                if (kvp.Key == "Windward Wall - z=0ft")
                {
                    wpr.Cp = 0.8;
                    wpr.GCpi_A = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.GCpi_B = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.z = 0;
                    wpr.Kz = Math.Round(WindLoadCalculator.GetKz(0.0, parameters.ExposureCategory), 2);
                    var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    wpr.qz = Math.Round(0.00256 * wpr.Kz * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    var qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    wpr.Pressure_A = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_A * qh, 2);
                    wpr.Pressure_B = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_B * qh, 2);
                }
                else if (kvp.Key == "Windward Wall - z=15ft")
                {
                    wpr.Cp = 0.8;
                    wpr.GCpi_A = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.GCpi_B = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.z = 15;
                    wpr.Kz = Math.Round(WindLoadCalculator.GetKz(15.0, parameters.ExposureCategory), 2);
                    var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    wpr.qz = Math.Round(0.00256 * wpr.Kz * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    var qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    wpr.Pressure_A = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_A * qh, 2);
                    wpr.Pressure_B = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_B * qh, 2);

                }
                else if (kvp.Key == "Windward Wall - z=h")
                {
                    wpr.Cp = 0.8;
                    wpr.GCpi_A = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.GCpi_B = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.z = parameters.BuildingHeight;
                    wpr.Kz = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    wpr.qz = Math.Round(0.00256 * wpr.Kz * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    var qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    wpr.Pressure_A = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_A * qh, 2);
                    wpr.Pressure_B = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_B * qh, 2);
                }
                else if (kvp.Key == "Leeward Wall")
                {
                    wpr.Cp = WindLoadCalculator.GetCpLeeward(parameters);
                    wpr.GCpi_A = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.GCpi_B = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.z = parameters.BuildingHeight;
                    wpr.Kz = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    wpr.qz = Math.Round(0.00256 * wpr.Kz * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    var qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    wpr.Pressure_A = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_A * qh, 2);
                    wpr.Pressure_B = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_B * qh, 2);
                }
                else if (kvp.Key == "Sidewall")
                {
                    wpr.Cp = WindLoadCalculator.GetCpSidewall(parameters);
                    wpr.GCpi_A = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.GCpi_B = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                    wpr.z = parameters.BuildingHeight;
                    wpr.Kz = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                    wpr.qz = Math.Round(0.00256 * wpr.Kz * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    var qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                    wpr.Pressure_A = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_A * qh, 2);
                    wpr.Pressure_B = Math.Round(wpr.qz * parameters.GustFactor * wpr.Cp + wpr.GCpi_B * qh, 2);
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
                GustFactor = 0.85,
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
        public double GustFactor { get; set; } = 0.85;
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


        public double z { get; set; }
        public double Kz { get; set; }
        public double GCpi_A { get; set; }  // GCpi for internal pressure expansion (balloon) case
        public double GCpi_B { get; set; }  // GCpi for internal pressure suction case
        public double qz { get; set; }

        public double Pressure_A { get; set; }
        public double Pressure_B { get; set; }

    }

    // Result class for roof zone pressure calculations
    public class RoofZonePressure
    {
        public string ZoneName { get; set; }
        public double StartDistance { get; set; }
        public double EndDistance { get; set; }
        public double Cp { get; set; }
        public double GCpi_A { get; set; }  // GCpi for internal pressure expansion (balloon) case
        public double GCpi_B { get; set; }  // GCpi for internal pressure suction case
        public double qz { get; set; }
        public double Pressure_A { get; set; }
        public double Pressure_B { get; set; }
    }

    // Wind Load Calculator class
    public static class WindLoadCalculator
    {
        public static Dictionary<string, double> CalculateMWFRS(WindLoadParameters p)
        {
            double V = p.WindSpeed;
            double h = p.BuildingHeight;

            if (h >= 15)
            {
                return new Dictionary<string, double>
                {
                    ["Windward Wall - z=0ft"] = 0,    // +GCpivalue for WW
                    ["Windward Wall - z=15ft"] = 0,  // +GCpivalue for WW
                    ["Windward Wall - z=h"] = 0,       // +GCpivalue for WW
                    ["Leeward Wall"] = 0,               // -GCpivalue for LW
                    ["Sidewall"] = 0,                      // -GCpivalue for SW
                };
            } else
            {
                return new Dictionary<string, double>
                {
                    ["Windward Wall - z=0ft"] = 0,
                    ["Windward Wall - z=h"] = 0,
                    ["Leeward Wall"] = 0,
                    ["Sidewall"] = 0,
                };
            }

        }

        // Calculate roof zone pressures based on distances and Cp values
        public static List<RoofZonePressure> CalculateRoofZonePressures(WindLoadParameters p)
        {
            List<RoofZonePressure> zones = new List<RoofZonePressure>();

            double h = p.BuildingHeight / 2.0; // Simplified for this example
            double L = p.BuildingLength;
            double qz = CalculateWindPressure(p);

            // Calculate 'a' for flat roofs or parallel ridge roofs per ASCE 7-16
            double a = Math.Min(0.1 * L, 0.4 * h);

            // Perpendicular to Wind (Windward and Leeward zones)
            if (p.RidgeDirection == "Parallel to Wind")
            {
                if (L <= 0.5 * h)
                {
                    // Windward Roof (single pressure)
                    zones.Add(new RoofZonePressure
                    {
                        ZoneName = "Windward Roof 0->h/2",
                        StartDistance = 0,
                        EndDistance = L,
                        Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                        qz = qz,


                        Cp = GetCpRoofWindward(p)
                    });
                }
                else
                {
                    // Windward Roof (single pressure)
                    zones.Add(new RoofZonePressure
                    {
                        ZoneName = "Windward Roof 0->h/2",
                        StartDistance = 0,
                        EndDistance = 0.5 * h,
                        Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                        Cp = GetCpRoofWindward(p)
                    });

                    if (L > 0.5 * h && L <= h)
                    {
                        zones.Add(new RoofZonePressure
                        {
                            ZoneName = "Windward Roof h/2->h",
                            StartDistance = 0.5 * h,
                            EndDistance = L,
                            Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                            Cp = GetCpRoofWindward(p)
                        });
                    }
                    else
                    {
                        zones.Add(new RoofZonePressure
                        {
                            ZoneName = "Windward Roof h/2->h",
                            StartDistance = 0.5 * h,
                            EndDistance = h,
                            Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                            Cp = GetCpRoofWindward(p)
                        });

                        if (L > h && L <= 2.0 * h)
                        {
                            zones.Add(new RoofZonePressure
                            {
                                ZoneName = "Windward Roof h->2h",
                                StartDistance = h,
                                EndDistance = L,
                                Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                                Cp = GetCpRoofWindward(p)
                            });
                        }
                        else
                        {
                            zones.Add(new RoofZonePressure
                            {
                                ZoneName = "Windward Roof h->2h",
                                StartDistance = h,
                                EndDistance = 2.0*h,
                                Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                                Cp = GetCpRoofWindward(p)
                            });

                            if(2.0 * h != h)
                            zones.Add(new RoofZonePressure
                            {
                                ZoneName = "Windward Roof 2h->end",
                                StartDistance = 2.0*h,
                                EndDistance = L,
                                Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                                Cp = GetCpRoofWindward(p)
                            });
                        }
                    }
                }

                // Now leeward roof values
                if (L <= 0.5 * h)
                {
                    // Windward Roof (single pressure)
                    zones.Add(new RoofZonePressure
                    {
                        ZoneName = "Leeward Roof 0->h/2",
                        StartDistance = 0,
                        EndDistance = L,
                        Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                        Cp = GetCpRoofWindward(p)
                    });
                }
                else
                {
                    // Windward Roof (single pressure)
                    zones.Add(new RoofZonePressure
                    {
                        ZoneName = "Leeward Roof 0->h/2",
                        StartDistance = 0,
                        EndDistance = 0.5 * h,
                        Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                        Cp = GetCpRoofWindward(p)
                    });

                    if (L > 0.5 * h && L <= h)
                    {
                        zones.Add(new RoofZonePressure
                        {
                            ZoneName = "Leeward Roof h/2->h",
                            StartDistance = 0.5 * h,
                            EndDistance = L,
                            Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                            Cp = GetCpRoofWindward(p)
                        });
                    }
                    else
                    {
                        zones.Add(new RoofZonePressure
                        {
                            ZoneName = "Leeward Roof h/2->h",
                            StartDistance = 0.5 * h,
                            EndDistance = h,
                            Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                            Cp = GetCpRoofWindward(p)
                        });

                        if (L > h && L <= 2.0 * h)
                        {
                            zones.Add(new RoofZonePressure
                            {
                                ZoneName = "Leeward Roof h->2h",
                                StartDistance = h,
                                EndDistance = L,
                                Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                                Cp = GetCpRoofWindward(p)
                            });
                        }
                        else
                        {
                            zones.Add(new RoofZonePressure
                            {
                                ZoneName = "Leeward Roof h->2h",
                                StartDistance = h,
                                EndDistance = 2.0 * h,
                                Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                                Cp = GetCpRoofWindward(p)
                            });

                            if (2.0 * h != h)
                                zones.Add(new RoofZonePressure
                                {
                                    ZoneName = "Leeward Roof 2h->end",
                                    StartDistance = 2.0 * h,
                                    EndDistance = L,
                                    Pressure_A = Math.Round(qz * -0.9, 2), // Cp for windward roof
                                    Cp = GetCpRoofWindward(p)
                                });
                        }
                    }
                }
            }
            // Parallel to Wind (Multiple zones like flat roof)
            else if (p.RidgeDirection == "Perpendicular to Wind")
            {
                // Zone 3: From 0 to 'a' (Edge zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 3 (WW Edge)",
                    StartDistance = 0,
                    EndDistance = a,
                    Pressure_A = Math.Round(qz * -1.3, 2), // Cp for edge zone
                    Cp = 0.0
                });

                // Zone 2: From 'a' to '2a' (Transition zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 2 (Transition)",
                    StartDistance = a,
                    EndDistance = 2 * a,
                    Pressure_A = Math.Round(qz * -1.0, 2), // Cp for transition zone
                    Cp = 0.0
                });

                // Zone 1: From '2a' to end of building (Interior zone)
                zones.Add(new RoofZonePressure
                {
                    ZoneName = "Zone 1 (LW Edge)",
                    StartDistance = 2 * a,
                    EndDistance = L,
                    Pressure_A = Math.Round(qz * -0.9, 2), // Cp for interior zone
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
        public static double GetGCpiMagnitude(string enclosure)
        {
            if (enclosure == "Enclosed") return 0.18;
            if (enclosure == "Partially Enclosed") return 0.55;
            if (enclosure == "Open") return 0.00;
            return 0.18;
        }

        // Get Kz approximation based on building height and exposure category
        public static double GetKz(double z, string exposure)
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









        public class RoofZoneCp
        {
            public string Zone { get; set; }
            public double hOverL { get; set; }
            public double StartDistance { get; set; }
            public double EndDistance { get; set; }
            public double CpCaseA { get; set; }
            public double CpCaseB { get; set; }
        }

        public static List<RoofZoneCp> CalculateFlatOrParallelRoofZones(double h, double L)
        {
            List<RoofZoneCp> zones = new List<RoofZoneCp>();
            double hOverL = h / L;

            var Cp_0_5 = new (string Zone, double CpA, double CpB)[]
            {
        ("Zone 1", -0.9, -0.18),
        ("Zone 2", -0.9, -0.18),
        ("Zone 3", -0.5, -0.18),
        ("Zone 4", -0.3, -0.18)
            };

            var Cp_1_0 = new (string Zone, double CpA, double CpB)[]
            {
        ("Zone 1", -1.3, -0.18),
        ("Zone 2", -0.7, -0.18),
            };

            // Helper to add a zone safely
            void AddZone(string zoneName, double start, double end, double cpA, double cpB)
            {
                if (start >= L) return; // Entire zone outside building, skip
                zones.Add(new RoofZoneCp
                {
                    Zone = zoneName,
                    StartDistance = Math.Min(start, L),
                    EndDistance = Math.Min(end, L),
                    CpCaseA = cpA,
                    CpCaseB = cpB
                });
            }

            if (hOverL <= 0.5)
            {
                AddZone("Zone 1", 0, 0.5 * h, Cp_0_5[0].CpA, Cp_0_5[0].CpB);
                AddZone("Zone 2", 0.5 * h, h, Cp_0_5[1].CpA, Cp_0_5[1].CpB);
                AddZone("Zone 3", h, 2.0 * h, Cp_0_5[2].CpA, Cp_0_5[2].CpB);
                AddZone("Zone 4", 2.0 * h, L, Cp_0_5[3].CpA, Cp_0_5[3].CpB);
            }
            else if (hOverL >= 1.0)
            {
                AddZone("Zone 1", 0, 0.5 * h, Cp_1_0[0].CpA, Cp_1_0[0].CpB);
                AddZone("Zone 2", 0.5 * h, L, Cp_1_0[1].CpA, Cp_1_0[1].CpB);
            }
            else
            {
                double t = (hOverL - 0.5) / 0.5;

                double Interpolate(double cpLow, double cpHigh) => cpLow + t * (cpHigh - cpLow);

                double Cp1A = Interpolate(Cp_0_5[0].CpA, Cp_1_0[0].CpA);
                double Cp1B = Interpolate(Cp_0_5[0].CpB, Cp_1_0[0].CpB);

                double Cp2A = Interpolate(Cp_0_5[1].CpA, Cp_1_0[1].CpA);
                double Cp2B = Interpolate(Cp_0_5[1].CpB, Cp_1_0[1].CpB);

                double Cp3A = Cp_0_5[2].CpA; // No interpolation for Zone 3
                double Cp3B = Cp_0_5[2].CpB;

                AddZone("Zone 1", 0, 0.5 * h, Cp1A, Cp1B);
                AddZone("Zone 2", 0.5 * h, h, Cp2A, Cp2B);
                AddZone("Zone 3", h, L, Cp3A, Cp3B);
            }

            return zones;
        }

        public static List<RoofZoneCp> CalculatePerpendicularToRidge_CpValue(double h, double L, double theta)
        {
            // Case A -- Figure 27.4.1 -- topmost values 
            Dictionary<double, double> RoofCoeff_hOVerL_0_25_CaseA_Windward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(10, -0.7);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(15, -0.5);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(20, -0.3);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(25, -0.2);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(30, -0.2);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(35, 0.0);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(45, 0.0);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(60, 0.0);
            RoofCoeff_hOVerL_0_25_CaseA_Windward.Add(90, 0.0);


            Dictionary<double, double> RoofCoeff_hOVerL_0_50_CaseA_Windward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(10, -0.9);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(15, -0.7);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(20, -0.4);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(25, -0.3);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(30, -0.2);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(35, -0.2);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(45, 0.0);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(60, 0.0);
            RoofCoeff_hOVerL_0_50_CaseA_Windward.Add(90, 0.0);


            Dictionary<double, double> RoofCoeff_hOVerL_1_0_CaseA_Windward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(10, -1.3);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(15, -1.0);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(20, -0.7);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(25, -0.5);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(30, -0.3);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(35, -0.2);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(45, 0.0);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(60, 0.0);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add(90, 0.0);


            Dictionary<double, Dictionary<double, double>> RoofCoefficientCp_CaseA_Windward = new Dictionary<double, Dictionary<double, double>>();
            RoofCoefficientCp_CaseA_Windward.Add(0.25, RoofCoeff_hOVerL_0_25_CaseA_Windward);
            RoofCoefficientCp_CaseA_Windward.Add(0.5, RoofCoeff_hOVerL_0_50_CaseA_Windward);
            RoofCoefficientCp_CaseA_Windward.Add(1.0, RoofCoeff_hOVerL_1_0_CaseA_Windward);

            Dictionary<double, double> RoofCoeff_hOVerL_0_25_CaseA_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_25_CaseA_Leeward.Add(10, -0.3);
            RoofCoeff_hOVerL_0_25_CaseA_Leeward.Add(15, -0.5);
            RoofCoeff_hOVerL_0_25_CaseA_Leeward.Add(20, -0.6);

            Dictionary<double, double> RoofCoeff_hOVerL_0_50_CaseA_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_50_CaseA_Leeward.Add(10, -0.5);
            RoofCoeff_hOVerL_0_50_CaseA_Leeward.Add(15, -0.5);
            RoofCoeff_hOVerL_0_50_CaseA_Leeward.Add(20, -0.6);

            Dictionary<double, double> RoofCoeff_hOVerL_1_0_CaseA_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_1_0_CaseA_Leeward.Add(10, -0.7);
            RoofCoeff_hOVerL_1_0_CaseA_Leeward.Add(15, -0.6);
            RoofCoeff_hOVerL_1_0_CaseA_Leeward.Add(20, -0.6);

            Dictionary<double, Dictionary<double, double>> RoofCoefficientCp_CaseA_Leeward = new Dictionary<double, Dictionary<double, double>>();
            RoofCoefficientCp_CaseA_Leeward.Add(0.25, RoofCoeff_hOVerL_0_25_CaseA_Leeward);
            RoofCoefficientCp_CaseA_Leeward.Add(0.5, RoofCoeff_hOVerL_0_50_CaseA_Leeward);
            RoofCoefficientCp_CaseA_Leeward.Add(1.0, RoofCoeff_hOVerL_1_0_CaseA_Leeward);


            // Case B -- Figure 27.4.1 -- bottommost values 
            Dictionary<double, double> RoofCoeff_hOVerL_0_25_CaseB_Windward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(10, -0.18);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(15, 0.0);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(20, 0.2);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(25, 0.3);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(30, 0.3);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(35, 0.4);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(45, 0.4);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(60, 0.01 * theta);
            RoofCoeff_hOVerL_0_25_CaseB_Windward.Add(90, 0.01 * theta);

            Dictionary<double, double> RoofCoeff_hOVerL_0_50_CaseB_Windward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(10, -0.18);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(15, -0.18);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(20, 0.0);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(25, 0.2);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(30, 0.2);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(35, 0.3);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(45, 0.4);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(60, 0.01 * theta);
            RoofCoeff_hOVerL_0_50_CaseB_Windward.Add(90, 0.01 * theta);

            Dictionary<double, double> RoofCoeff_hOVerL_1_0_CaseB_Windward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(10, -0.18);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(15, -0.18);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(20, -0.18);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(25, 0.0);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(30, 0.2);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(35, 0.2);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(45, 0.3);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(60, 0.01 * theta);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add(90, 0.01 * theta);

            Dictionary<double, Dictionary<double, double>> RoofCoefficientCp_CaseB_Windward = new Dictionary<double, Dictionary<double, double>>();
            RoofCoefficientCp_CaseB_Windward.Add(0.25, RoofCoeff_hOVerL_0_25_CaseB_Windward);
            RoofCoefficientCp_CaseB_Windward.Add(0.5, RoofCoeff_hOVerL_0_50_CaseB_Windward);
            RoofCoefficientCp_CaseB_Windward.Add(1.0, RoofCoeff_hOVerL_1_0_CaseB_Windward);

            Dictionary<double, double> RoofCoeff_hOVerL_0_25_CaseB_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(10, -0.3);
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(15, -0.5);
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(20, -0.6);

            Dictionary<double, double> RoofCoeff_hOVerL_0_50_CaseB_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(10, -0.5);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(15, -0.5);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(20, -0.6);

            Dictionary<double, double> RoofCoeff_hOVerL_1_0_CaseB_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(10, -0.7);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(15, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(20, -0.6);

            Dictionary<double, Dictionary<double, double>> RoofCoefficientCp_CaseB_Leeward = new Dictionary<double, Dictionary<double, double>>();
            RoofCoefficientCp_CaseB_Leeward.Add(0.25, RoofCoeff_hOVerL_0_25_CaseB_Leeward);
            RoofCoefficientCp_CaseB_Leeward.Add(0.5, RoofCoeff_hOVerL_0_50_CaseB_Leeward);
            RoofCoefficientCp_CaseB_Leeward.Add(1.0, RoofCoeff_hOVerL_1_0_CaseB_Leeward);

            List<RoofZoneCp> zones = new List<RoofZoneCp>();
            double hOverL = h / L;

            double t_hOverL;
            double hOverL_low;
            double hOverL_high;
            double t_theta;
            double theta_low;
            double theta_high;
            // Interpolate for h/L
            if (hOverL > 0.25 && hOverL < 0.5)
            {
                // Interpolate for h/L between 0.25 and 0.5
                t_hOverL = (hOverL - 0.25) / (1.0 - 0.25); // t = 0 at h/L = 0.25, t = 1 at h/L = 1.0
                hOverL_low = 0.25;
                hOverL_high = 0.5;
            }
            else
            {
                // Interpolate for h/L between 0.5 and 1.0
                t_hOverL = (hOverL - 0.5) / (1.0 - 0.5); // t = 0 at h/L = 0.5, t = 1 at h/L = 1.0
                hOverL_low = 0.5;
                hOverL_high = 1.0;
            }

            // Interpolate for theta
            if (theta >= 10 && theta < 15)
            {
                t_theta = (theta - 10) / (15 - 10); // t = 0 at theta = 10, t = 1 at theta = 15
                theta_low = 10;
                theta_high = 15;
            }
            else if (theta >= 15 && theta < 20)
            {
                t_theta = (theta - 15) / (20 - 15); // t = 0 at theta = 15, t = 1 at theta = 20
                theta_low = 15;
                theta_high = 20;
            }
            else if (theta >= 20 && theta < 25)
            {
                t_theta = (theta - 20) / (25 - 20); // t = 0 at theta = 20, t = 1 at theta = 25
                theta_low = 20;
                theta_high = 25;
            }
            else if (theta >= 25 && theta < 30)
            {
                t_theta = (theta - 25) / (30 - 25); // t = 0 at theta = 25, t = 1 at theta = 30
                theta_low = 25;
                theta_high = 30;
            }
            else if (theta >= 30 && theta < 35)
            {
                t_theta = (theta - 30) / (35 - 30); // t = 0 at theta = 30, t = 1 at theta = 35
                theta_low = 30;
                theta_high = 35;
            }
            else if (theta >= 35 && theta < 45)
            {
                t_theta = (theta - 35) / (45 - 35); // t = 0 at theta = 35, t = 1 at theta = 40
                theta_low = 35;
                theta_high = 45;
            }
            else if (theta >= 45 && theta < 60)
            {
                t_theta = (theta - 45) / (60 - 45); // t = 0 at theta = 45, t = 1 at theta = 60
                theta_low = 45;
                theta_high = 60;
            }
            else
            {
                t_theta = (theta - 60) / (90 - 60); // t = 0 at theta = 60, t = 1 at theta = 90
                theta_low = 60;
                theta_high = 90;
            }

            // WINDWARD INTERPOLATIONS
            //Case A Windward interpolation
            double low_lowCp_theta_CaseA_Windward = RoofCoefficientCp_CaseA_Windward[hOverL_low][theta_low];
            double low_highCp_theta_CaseA_Windward = RoofCoefficientCp_CaseA_Windward[hOverL_low][theta_high];
            double high_lowCp_theta_CaseA_Windward = RoofCoefficientCp_CaseA_Windward[hOverL_high][theta_low];
            double high_highCp_theta_CaseA_Windward = RoofCoefficientCp_CaseA_Windward[hOverL_high][theta_high];

            // interpolated values for theta
            double interpolated_low_Cp_theta_CaseA_Windward = low_lowCp_theta_CaseA_Windward + t_theta * (low_highCp_theta_CaseA_Windward - low_lowCp_theta_CaseA_Windward);
            double interpolated_high_Cp_theta_CaseA_Windward = high_lowCp_theta_CaseA_Windward + t_theta * (high_highCp_theta_CaseA_Windward - high_lowCp_theta_CaseA_Windward);

            // then interpolate values for h/L
            double interpolated_Cp_CaseA_Windward = interpolated_low_Cp_theta_CaseA_Windward + t_hOverL * (interpolated_high_Cp_theta_CaseA_Windward - interpolated_low_Cp_theta_CaseA_Windward);

            // Case B interpolation
            double low_lowCp_theta_CaseB_Windward = RoofCoefficientCp_CaseB_Windward[hOverL_low][theta_low];
            double low_highCp_theta_CaseB_Windward = RoofCoefficientCp_CaseB_Windward[hOverL_low][theta_high];
            double high_lowCp_theta_CaseB_Windward = RoofCoefficientCp_CaseB_Windward[hOverL_high][theta_low];
            double high_highCp_theta_CaseB_Windward = RoofCoefficientCp_CaseB_Windward[hOverL_high][theta_high];

            // interpolated values for theta
            double interpolated_low_Cp_theta_CaseB_Windward = low_lowCp_theta_CaseB_Windward + t_theta * (low_highCp_theta_CaseB_Windward - low_lowCp_theta_CaseB_Windward);
            double interpolated_high_Cp_theta_CaseB_Windward = high_lowCp_theta_CaseB_Windward + t_theta * (high_highCp_theta_CaseB_Windward - high_lowCp_theta_CaseB_Windward);

            // then interpolate values for h/L
            double interpolated_Cp_CaseB_Windward = interpolated_low_Cp_theta_CaseB_Windward + t_hOverL * (interpolated_high_Cp_theta_CaseB_Windward - interpolated_low_Cp_theta_CaseB_Windward);

            // LEEWARD INTERPOLATIONS
            //Case A Windward interpolation
            double low_lowCp_theta_CaseA_Leeward = RoofCoefficientCp_CaseA_Leeward[hOverL_low][theta_low];
            double low_highCp_theta_CaseA_Leeward = RoofCoefficientCp_CaseA_Leeward[hOverL_low][theta_high];
            double high_lowCp_theta_CaseA_Leeward = RoofCoefficientCp_CaseA_Leeward[hOverL_high][theta_low];
            double high_highCp_theta_CaseA_Leeward = RoofCoefficientCp_CaseA_Leeward[hOverL_high][theta_high];

            // interpolated values for theta
            double interpolated_low_Cp_theta_CaseA_Leeward = low_lowCp_theta_CaseA_Leeward + t_theta * (low_highCp_theta_CaseA_Leeward - low_lowCp_theta_CaseA_Leeward);
            double interpolated_high_Cp_theta_CaseA_Leeward = high_lowCp_theta_CaseA_Leeward + t_theta * (high_highCp_theta_CaseA_Leeward - high_lowCp_theta_CaseA_Leeward);

            // then interpolate values for h/L
            double interpolated_Cp_CaseA_Leeward = interpolated_low_Cp_theta_CaseA_Leeward + t_hOverL * (interpolated_high_Cp_theta_CaseA_Leeward - interpolated_low_Cp_theta_CaseA_Leeward);

            // Case B interpolation
            double low_lowCp_theta_CaseB_Leeward = RoofCoefficientCp_CaseB_Leeward[hOverL_low][theta_low];
            double low_highCp_theta_CaseB_Leeward = RoofCoefficientCp_CaseB_Leeward[hOverL_low][theta_high];
            double high_lowCp_theta_CaseB_Leeward = RoofCoefficientCp_CaseB_Leeward[hOverL_high][theta_low];
            double high_highCp_theta_CaseB_Leeward = RoofCoefficientCp_CaseB_Leeward[hOverL_high][theta_high];

            // interpolated values for theta
            double interpolated_low_Cp_theta_CaseB_Leeward = low_lowCp_theta_CaseB_Leeward + t_theta * (low_highCp_theta_CaseB_Leeward - low_lowCp_theta_CaseB_Leeward);
            double interpolated_high_Cp_theta_CaseB_Leeward = high_lowCp_theta_CaseB_Leeward + t_theta * (high_highCp_theta_CaseB_Leeward - high_lowCp_theta_CaseB_Leeward);

            // then interpolate values for h/L
            double interpolated_Cp_CaseB_Leeward = interpolated_low_Cp_theta_CaseB_Leeward + t_hOverL * (interpolated_high_Cp_theta_CaseB_Leeward - interpolated_low_Cp_theta_CaseB_Leeward);



            // Zone Definitions based on h/L ratio
            zones.Add(new RoofZoneCp
            {
                Zone = "Windward Zone 1",
                hOverL = hOverL,
                StartDistance = 0,
                EndDistance = 0.5 * L,
                CpCaseA = interpolated_Cp_CaseA_Windward,
                CpCaseB = interpolated_Cp_CaseB_Windward
            });

            zones.Add(new RoofZoneCp
            {
                Zone = "Leeward Zone 1",
                StartDistance = 0.5 * L,
                EndDistance = L,
                CpCaseA = interpolated_Cp_CaseA_Leeward,
                CpCaseB = interpolated_Cp_CaseB_Leeward
            });

            return zones;
        }

        // Windward Cp values based on h/L, theta
        private static Tuple<double, double> GetCpWindwardPerpendicular(double hOverL, double theta)
        {
            // Windward Cp values for specific h/L ratios
            if (hOverL <= 0.25)
            {
                // Example Cp values for h/L <= 0.25 and theta interpolation
                return new Tuple<double, double>(-0.9, -0.9); // Example values
            }
            else if (hOverL == 0.5)
            {
                // Example Cp values for h/L = 0.5 and theta interpolation
                return new Tuple<double, double>(-1.0, -1.0); // Example values
            }
            else if (hOverL >= 1.0)
            {
                // Example Cp values for h/L >= 1.0 and theta interpolation
                return new Tuple<double, double>(-1.3, -1.3); // Example values
            }

            return new Tuple<double, double>(0.0, 0.0); // Default values (should never hit this)
        }

        // Leeward Cp values based on h/L, theta
        private static Tuple<double, double> GetCpLeewardPerpendicular(double hOverL, double theta)
        {
            if (hOverL <= 0.25)
            {
                // Return Cp values for leeward for h/L <= 0.25 and given theta
                return new Tuple<double, double>(-0.5, -0.18); // Example values
            }
            else if (hOverL == 0.5)
            {
                // Return Cp values for leeward for h/L = 0.5 and given theta
                return new Tuple<double, double>(-0.7, -0.18); // Example values
            }
            else if (hOverL >= 1.0)
            {
                // Return Cp values for leeward for h/L >= 1.0 and given theta
                return new Tuple<double, double>(-1.0, -0.18); // Example values
            }

            return new Tuple<double, double>(0.0, 0.0); // Default values (should never hit this)
        }

        // Interpolation for Cp values based on theta
        private static double InterpolateCpValues(double[] CpValues, double theta)
        {
            // Angles corresponding to the CpValues in the order (10, 15, 20, 25, 30, 35, 45, 60+)
            double[] angles = new double[] { 10, 15, 20, 25, 30, 35, 45, 60 };

            // Find the two closest angles to interpolate between
            for (int i = 0; i < angles.Length - 1; i++)
            {
                if (theta >= angles[i] && theta <= angles[i + 1])
                {
                    double t = (theta - angles[i]) / (angles[i + 1] - angles[i]);
                    return CpValues[i] + t * (CpValues[i + 1] - CpValues[i]);
                }
            }

            return CpValues[0]; // Default value
        }

        private static void AdjustZoneEndDistances(ref List<RoofZoneCp> zones, double buildingLength)
        {
            // Adjust zones so they don't exceed the building length
            foreach (var zone in zones)
            {
                if (zone.EndDistance > buildingLength)
                {
                    zone.EndDistance = buildingLength;
                }
            }
        }

    }
}
