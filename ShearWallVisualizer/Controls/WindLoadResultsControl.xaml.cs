using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadResultsControl : UserControl
    {
        private WindLoadParameters _parameters;

        public WindLoadResultsControl()
        {
            
        }

        public WindLoadResultsControl(WindLoadParameters parameters)
        {
            InitializeComponent();

            _parameters = parameters;

            Dictionary<string, double> wall_zones = WindLoadCalculator.CalculateMWFRS_WallZones(parameters);
            Dictionary<string, double> roof_zones = WindLoadCalculator.CalculateMWFRS_RoofZones(parameters);

            // Display wall results in the DataGrid

            List<WindPressurResult_Wall> wall_results = CalculateWallPressureResults(parameters, wall_zones);
            List<WindPressurResult_Roof> roof_results = CalculateRoofPressureResults(parameters, roof_zones);


            tbl_qh.Text = Math.Round(WindLoadCalculator.CalculateWindPressure(parameters, parameters.BuildingHeight), 2).ToString();
            tbl_theta.Text = Math.Round(parameters.RoofPitch, 2).ToString();
            tbl_hOverL.Text = Math.Round(parameters.BuildingHeight / parameters.BuildingLength, 2).ToString();
            tbl_h.Text = Math.Round(parameters.BuildingHeight, 2).ToString();
            tbl_windOrientation.Text = parameters.RidgeDirection;

            WallResultsDataGrid.ItemsSource = null;
            WallResultsDataGrid.ItemsSource = wall_results;

            RoofResultsDataGrid.ItemsSource = null;
            RoofResultsDataGrid.ItemsSource = roof_results;
        }

        private static List<WindPressurResult_Wall> CalculateWallPressureResults(WindLoadParameters parameters, Dictionary<string, double> wall_zones)
        {
            List<WindPressurResult_Wall> wall_results = new List<WindPressurResult_Wall>();

            // Helper method to calculate common values for pressure calculation
            double CalculatePressure(double qz, double qh, double Cp, double GCpi, double GustFactor)
            {
                return  Math.Round(qz * GustFactor * Cp + GCpi * qh, 2);
            }

            // Loop through wall zones and calculate pressures
            foreach (var kvp in wall_zones)
            {
                WindPressurResult_Wall wpr = new WindPressurResult_Wall();
                wpr.Surface = kvp.Key;

                // Get the correct Cp value based on the surface type
                wpr.GCpi_A = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification); // balloon case
                wpr.GCpi_B = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification); // internal suction case

                switch (kvp.Key)
                {
                    case "Windward Wall - z=0ft":
                        wpr.z = 0;
                        wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
                        break;
                    case "Windward Wall - z=15ft":
                        wpr.z = 15;
                        wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
                        break;
                    case "Windward Wall - z=h":
                        wpr.z = parameters.BuildingHeight;
                        wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
                        break;

                    case "Leeward Wall":
                        // swap these values since the balloon case is additive for leeward and sidewalls
                        double temp = wpr.GCpi_A;
                        wpr.GCpi_A = wpr.GCpi_B;
                        wpr.GCpi_B = temp;

                        wpr.z = parameters.BuildingHeight;
                        wpr.Cp = WindLoadCalculator.GetCpLeewardWall(parameters);
                        break;

                    case "Sidewall":
                        // swap these values since the balloon case is additive for leeward and sidewalls
                        double temp2 = wpr.GCpi_A;
                        wpr.GCpi_A = wpr.GCpi_B;
                        wpr.GCpi_B = temp2;

                        wpr.z = parameters.BuildingHeight;
                        wpr.Cp = WindLoadCalculator.GetCpSidewall(parameters);
                        break;

                    default:
                        wpr.GCpi_A = 0;
                        wpr.GCpi_B = 0;
                        wpr.z = 0;
                        wpr.Cp = 0; // Default value, can be adjusted based on use case
                        break;
                }

                // Common values
                wpr.Kz = Math.Round(WindLoadCalculator.GetKz(wpr.z, parameters.ExposureCategory), 2);
                var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                wpr.qz = Math.Round(0.00256 * wpr.Kz * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                var qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);

                // Calculate pressures using common logic
                wpr.Pressure_A = CalculatePressure(wpr.qz, qh, wpr.Cp, wpr.GCpi_A, parameters.GustFactor);
                wpr.Pressure_B = CalculatePressure(wpr.qz, qh, wpr.Cp, wpr.GCpi_B, parameters.GustFactor);

                // Add the result to the list
                wall_results.Add(wpr);
            }

            return wall_results;
        }

        private static List<WindPressurResult_Roof> CalculateRoofPressureResults(WindLoadParameters parameters, Dictionary<string, double> roof_zones)
        {
            List<WindPressurResult_Roof> roof_results = new List<WindPressurResult_Roof>();
            foreach (var kvp in roof_zones)
            {
                WindPressurResult_Roof wpr = new WindPressurResult_Roof();
                wpr.Surface = kvp.Key;

                RoofCpCases cases;
                if(parameters.RidgeDirection == "Perpendicular to Wind")
                {
                    if(parameters.RoofPitch >= 10.0)
                    {
                       cases = WindLoadCalculator.CalculateRoofCp_PerpendicularRidge(parameters.BuildingHeight, parameters.BuildingLength, parameters.RoofPitch);
                    } else
                    {
                        cases = WindLoadCalculator.CalculateRoofCp_ForFlatRoofOrParallelRidge(parameters.BuildingHeight, parameters.BuildingLength, kvp.Key);
                    }
                } else
                {
                    cases = WindLoadCalculator.CalculateRoofCp_ForFlatRoofOrParallelRidge(parameters.BuildingHeight, parameters.BuildingLength, kvp.Key);
                }

                if (kvp.Key == "Windward Roof 0->h/2")
                {
                    wpr.Start = 0;
                    wpr.End = parameters.BuildingHeight / 2;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }
                else if (kvp.Key == "Windward Roof h/2->h")
                {
                    wpr.Start = parameters.BuildingHeight / 2;
                    wpr.End = parameters.BuildingHeight;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }
                else if (kvp.Key == "Windward Roof h->2h")
                {
                    wpr.Start = parameters.BuildingHeight;
                    wpr.End = 2 * parameters.BuildingHeight;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }
                else if (kvp.Key == "Windward Roof 2h->end")
                {
                    wpr.Start = 2 * parameters.BuildingHeight;
                    wpr.End = parameters.BuildingLength;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }

                else if (kvp.Key == "Windward Roof")
                {
                    wpr.Start = 0.0;
                    wpr.End = parameters.BuildingLength / 2;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }
                else if (kvp.Key == "Leeward Roof")
                {
                    wpr.Start = parameters.BuildingLength / 2;
                    wpr.End = parameters.BuildingLength;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Leeward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Leeward, 2);
                }
                 else
                {
                    wpr.Start = 0;
                    wpr.End = 0;
                    wpr.CpA = 0;
                    wpr.CpB = 0;
                }

                // Adjust the end point if it's off the building length
                if (wpr.End > parameters.BuildingLength)
                {
                    wpr.End = parameters.BuildingLength;
                }



                wpr.GCpi_A = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                wpr.GCpi_B = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
                var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
                wpr.qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
                wpr.Pressure_A1 = Math.Round(wpr.qh * parameters.GustFactor * wpr.CpA + wpr.GCpi_A * wpr.qh, 2);
                wpr.Pressure_A2 = Math.Round(wpr.qh * parameters.GustFactor * wpr.CpA - wpr.GCpi_A * wpr.qh, 2);
                wpr.Pressure_B1 = Math.Round(wpr.qh * parameters.GustFactor * wpr.CpB + wpr.GCpi_B * wpr.qh, 2);
                wpr.Pressure_B2 = Math.Round(wpr.qh * parameters.GustFactor * wpr.CpB - wpr.GCpi_B * wpr.qh, 2);
                wpr.theta = parameters.RoofPitch;


                roof_results.Add(wpr);


            }

            return roof_results;
        }
    }

    // Result class for wall wind pressure calculations
    public class WindPressurResult_Wall
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

    // Result class for roof wind pressure calculations
    public class WindPressurResult_Roof
    {
        public string Surface { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public double CpA { get; set; }
        public double CpB { get; set; }

        public double theta { get; set; }
        public double GCpi_A { get; set; }  // GCpi for internal pressure expansion (balloon) case
        public double GCpi_B { get; set; }  // GCpi for internal pressure suction case
        public double qh { get; set; }
        public double Pressure_A1 { get; set; }
        public double Pressure_A2 { get; set; }

        public double Pressure_B1 { get; set; }
        public double Pressure_B2 { get; set; }

    }

    // Wind Load Calculator class
    public static class WindLoadCalculator
    {
        public static Dictionary<string, double> CalculateMWFRS_WallZones(WindLoadParameters p)
        {
            double V = p.WindSpeed;
            double h = p.BuildingHeight;

            if (h >= 15)
            {
                return new Dictionary<string, double>
                {
                    ["Windward Wall - z=0ft"] = 0,
                    ["Windward Wall - z=15ft"] = 0,
                    ["Windward Wall - z=h"] = 0,
                    ["Leeward Wall"] = 0,
                    ["Sidewall"] = 0,
                };
            }
            else
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

        public static Dictionary<string, double> CalculateMWFRS_RoofZones(WindLoadParameters p)
        {
            double theta = p.RoofPitch;
            double V = p.WindSpeed;
            double h = p.BuildingHeight;
            double L = p.BuildingLength;
            double hOverL = h / L;

            Dictionary<string, double> zones = new Dictionary<string, double>();

            if (p.RidgeDirection == "Perpendicular to Wind")
            {
                if (theta < 10)
                {
                    if (L <= 0.5 * h)
                    {
                        // Windward Roof (single pressure)
                        zones.Add("Windward Roof 0->h/2", 0);

                    }
                    else if (L > 0.5 * h && L <= h)
                    {
                        zones.Add("Windward Roof 0->h/2", 0);
                        zones.Add("Windward Roof h/2->h", 0);
                    }
                    else if (L > h && L <= 2.0 * h)
                    {
                        zones.Add("Windward Roof 0->h/2", 0);
                        zones.Add("Windward Roof h/2->h", 0);
                        zones.Add("Windward Roof h->2h", 0);
                    }
                    else
                    {
                        zones.Add("Windward Roof 0->h/2", 0);
                        zones.Add("Windward Roof h/2->h", 0);
                        zones.Add("Windward Roof h->2h", 0);
                        zones.Add("Windward Roof 2h->end", 0);
                    }
                }
                else
                {
                    zones.Add("Windward Roof", 0);
                    zones.Add("Leeward Roof", 0);
                }
            }
            else
            {
                if (L <= 0.5 * h)
                {
                    // Windward Roof (single pressure)
                    zones.Add("Windward Roof 0->h/2", 0);
                }
                else if (L > 0.5 * h && L <= h)
                {
                    zones.Add("Windward Roof 0->h/2", 0);
                    zones.Add("Windward Roof h/2->h", 0);
                }
                else if (L > h && L <= 2.0 * h)
                {
                    zones.Add("Windward Roof 0->h/2", 0);
                    zones.Add("Windward Roof h/2->h", 0);
                    zones.Add("Windward Roof h->2h", 0);
                }
                else
                {
                    zones.Add("Windward Roof 0->h/2", 0);
                    zones.Add("Windward Roof h/2->h", 0);
                    zones.Add("Windward Roof h->2h", 0);
                    zones.Add("Windward Roof 2h->end", 0);
                }
            }

            return zones;
        }

        // Calculate wind pressure based on wind speed and other parameters
        public static double CalculateWindPressure(WindLoadParameters p, double z)
        {
            double V = p.WindSpeed;
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

        // Cp values for different surfaces
        public static double GetCpLeewardWall(WindLoadParameters p)
        {
            double length = p.BuildingLength;
            double width = p.BuildingWidth;

            // Ensure ratio is length over width (L/B) ≥ 1
            double ratio = length / width; ;

            // Approximate Cp values based on ASCE 7-16 Figure 27.4-1
            if (ratio < 2.0)
                return -0.5;
            else if (ratio < 4.0)
                return -0.3;
            else
                return -0.2;
        }
        public static double GetCpSidewall(WindLoadParameters p) => -0.7;
        public static double GetCpWindwardwall(WindLoadParameters p) => 0.8;

        public static RoofCpCases CalculateRoofCp_ForFlatRoofOrParallelRidge(double h, double L, string zone_name)
        {
            double hOverL = h / L;

            // Define the base Cp values for the roof coefficients at different hOverL ranges
            Dictionary<string, double> RoofCoeff_hOVerL_0_5_CaseA_Windward = new Dictionary<string, double>
            {
                { "Windward Roof 0->h/2", -0.9 },
                { "Windward Roof h/2->h", -0.9 },
                { "Windward Roof h->2h", -0.5 },
                { "Windward Roof 2h->end", -0.3 }
            };

            Dictionary<string, double> RoofCoeff_hOVerL_0_5_CaseB_Windward = new Dictionary<string, double>
            {
                { "Windward Roof 0->h/2", -0.18 },
                { "Windward Roof h/2->h", -0.18 },
                { "Windward Roof h->2h", -0.18 },
                { "Windward Roof 2h->end", -0.18 }
            };

            Dictionary<string, double> RoofCoeff_hOVerL_1_0_CaseA_Windward = new Dictionary<string, double>
            {
                { "Windward Roof 0->h/2", -1.3 },
                { "Windward Roof h/2->h", -0.7 },
                { "Windward Roof h->2h", -0.7 },
                { "Windward Roof 2h->end", -0.7 }
            };

            Dictionary<string, double> RoofCoeff_hOVerL_1_0_CaseB_Windward = new Dictionary<string, double>
            {
                { "Windward Roof 0->h/2", -0.18 },
                { "Windward Roof h/2->h", -0.18 },
                { "Windward Roof h->2h", -0.18 },
                { "Windward Roof 2h->end", -0.18 }
            };

            double CpA, CpB;

            // Interpolation based on hOverL values (only)
            if (hOverL <= 0.5)
            {
                CpA = RoofCoeff_hOVerL_0_5_CaseA_Windward.ContainsKey(zone_name) ? RoofCoeff_hOVerL_0_5_CaseA_Windward[zone_name] : 0.0;
                CpB = RoofCoeff_hOVerL_0_5_CaseB_Windward.ContainsKey(zone_name) ? RoofCoeff_hOVerL_0_5_CaseB_Windward[zone_name] : 0.0;
            }
            else if (hOverL > 0.5 && hOverL <= 1.0)
            {
                // Interpolate Cp values for hOverL range (0.5 to 1.0)
                double t = (hOverL - 0.5) / (1.0 - 0.5);
                CpA = RoofCoeff_hOVerL_0_5_CaseA_Windward.ContainsKey(zone_name) ? InterpolateCp(RoofCoeff_hOVerL_0_5_CaseA_Windward[zone_name], RoofCoeff_hOVerL_1_0_CaseA_Windward[zone_name], 0.5, 1.0, t) : 0.0;
                CpB = RoofCoeff_hOVerL_0_5_CaseA_Windward.ContainsKey(zone_name) ? InterpolateCp(RoofCoeff_hOVerL_0_5_CaseB_Windward[zone_name], RoofCoeff_hOVerL_1_0_CaseB_Windward[zone_name], 0.5, 1.0, t) : 0.0;
            }
            else
            {
                CpA = RoofCoeff_hOVerL_1_0_CaseA_Windward.ContainsKey(zone_name) ? RoofCoeff_hOVerL_1_0_CaseA_Windward[zone_name] : 0.0;
                CpB = RoofCoeff_hOVerL_1_0_CaseB_Windward.ContainsKey(zone_name) ? RoofCoeff_hOVerL_1_0_CaseB_Windward[zone_name] : 0.0;
            }

            // Return the Cp values wrapped in RoofCpCases (Leeward values are not relevant for flat roofs)
            return new RoofCpCases(CpA, CpB, 0, 0);  // No leeward surfaces on a flat roof
        }

        /// <summary>
        /// Determines interpolated Cp values for normal to ridge with thetat >= 10degs.
        /// ASCE7-10 Figure 27.4-1
        /// </summary>
        /// <param name="h"></param>
        /// <param name="L"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static RoofCpCases CalculateRoofCp_PerpendicularRidge(double h, double L, double theta)
        {
            // Define roof coefficients for different cases and wind directions
            var roofCoefficients = new Dictionary<string, Dictionary<double, Dictionary<double, double>>>
            {
                {
                    "CaseA_Windward", new Dictionary<double, Dictionary<double, double>>()
                    {
                        { 0.25, new Dictionary<double, double> { { 10, -0.7 }, { 15, -0.5 }, { 20, -0.3 }, { 25, -0.2 }, { 30, -0.2 }, { 35, 0.0 }, { 45, 0.0 }, { 60, 0.0 }, { 90, 0.0 } } },
                        { 0.5, new Dictionary<double, double> { { 10, -0.9 }, { 15, -0.7 }, { 20, -0.4 }, { 25, -0.3 }, { 30, -0.2 }, { 35, -0.2 }, { 45, 0.0 }, { 60, 0.0 }, { 90, 0.0 } } },
                        { 1.0, new Dictionary<double, double> { { 10, -1.3 }, { 15, -1.0 }, { 20, -0.7 }, { 25, -0.5 }, { 30, -0.3 }, { 35, -0.2 }, { 45, 0.0 }, { 60, 0.0 }, { 90, 0.0 } } }
                    }
                },
                {
                    "CaseA_Leeward", new Dictionary<double, Dictionary<double, double>>()
                    {
                        { 0.25, new Dictionary<double, double> { { 10, -0.3 }, { 15, -0.5 }, { 20, -0.6 } } },
                        { 0.5, new Dictionary<double, double> { { 10, -0.5 }, { 15, -0.5 }, { 20, -0.6 } } },
                        { 1.0, new Dictionary<double, double> { { 10, -0.7 }, { 15, -0.6 }, { 20, -0.6 } } }
                    }
                },
                {
                    "CaseB_Windward", new Dictionary<double, Dictionary<double, double>>()
                    {
                        { 0.25, new Dictionary<double, double> { { 10, -0.18 }, { 15, 0.0 }, { 20, 0.2 }, { 25, 0.3 }, { 30, 0.3 }, { 35, 0.4 }, { 45, 0.4 }, { 60, 0.01 }, { 90, 0.01 } } },
                        { 0.5, new Dictionary<double, double> { { 10, -0.18 }, { 15, -0.18 }, { 20, 0.0 }, { 25, 0.2 }, { 30, 0.2 }, { 35, 0.3 }, { 45, 0.4 }, { 60, 0.01 }, { 90, 0.01 } } },
                        { 1.0, new Dictionary<double, double> { { 10, -0.18 }, { 15, -0.18 }, { 20, -0.18 }, { 25, 0.0 }, { 30, 0.2 }, { 35, 0.2 }, { 45, 0.3 }, { 60, 0.01 }, { 90, 0.01 } } }
                    }
                },
                {
                    "CaseB_Leeward", new Dictionary<double, Dictionary<double, double>>()
                    {
                        { 0.25, new Dictionary<double, double> { { 10, -0.3 }, { 15, -0.5 }, { 20, -0.6 }, { 25, -0.6 }, { 30, -0.6 }, { 35, -0.6 }, { 45, -0.6 }, { 60, -0.6 }, { 90, -0.6 } } },
                        { 0.5, new Dictionary<double, double> { { 10, -0.5 }, { 15, -0.5 }, { 20, -0.6 }, { 25, -0.6 }, { 30, -0.6 }, { 35, -0.6 }, { 45, -0.6 }, { 60, -0.6 }, { 90, -0.6 } } },
                        { 1.0, new Dictionary<double, double> { { 10, -0.7 }, { 15, -0.6 }, { 20, -0.6 }, { 25, -0.6 }, { 30, -0.6 }, { 35, -0.6 }, { 45, -0.6 }, { 60, -0.6 }, { 90, -0.6 } } }
                    }
                }
            };

            // Compute h/L ratio
            double hOverL = h / L;

            // Determine closest hOverL ranges (0.25, 0.5, or 1.0)
            double lowHOverL = GetClosestHOverLRange(hOverL);
            double highHOverL = lowHOverL == 0.25 ? 0.5 : (lowHOverL == 0.5 ? 1.0 : 1.0);

            // Calculate interpolation factor for hOverL
            double t_hOverL = (hOverL - lowHOverL) / (highHOverL - lowHOverL);

            // Interpolate theta
            double[] thetaBreaks = { 10, 15, 20, 25, 30, 35, 45, 60, 90 };
            double theta_low = thetaBreaks.First(x => x >= theta);
            double theta_high = thetaBreaks.Last(x => x <= theta);

            double t_theta;
            if (theta_low == theta_high)
            {
                t_theta = 0.0;
            }
            else
            {
                t_theta = (theta - theta_low) / (theta_high - theta_low);
            }

            // Select windward or leeward case for both A and B
            string caseKeyA_Windward = "CaseA_Windward";
            string caseKeyA_Leeward = "CaseA_Leeward";
            string caseKeyB_Windward = "CaseB_Windward";
            string caseKeyB_Leeward = "CaseB_Leeward";

            var caseDataA_Windward = roofCoefficients[caseKeyA_Windward];
            var caseDataA_Leeward = roofCoefficients[caseKeyA_Leeward];
            var caseDataB_Windward = roofCoefficients[caseKeyB_Windward];
            var caseDataB_Leeward = roofCoefficients[caseKeyB_Leeward];

            // Step 1: Interpolate over theta for each h/L
            double CpA_Windward_LowTheta = InterpolateCp(
                caseDataA_Windward[lowHOverL][theta_low],
                caseDataA_Windward[lowHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );
            double CpA_Windward_HighTheta = InterpolateCp(
                caseDataA_Windward[highHOverL][theta_low],
                caseDataA_Windward[highHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );

            double CpA_Leeward_LowTheta = InterpolateCp(
                caseDataA_Leeward[lowHOverL][theta_low],
                caseDataA_Leeward[lowHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );
            double CpA_Leeward_HighTheta = InterpolateCp(
                caseDataA_Leeward[highHOverL][theta_low],
                caseDataA_Leeward[highHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );

            double CpB_Windward_LowTheta = InterpolateCp(
                caseDataB_Windward[lowHOverL][theta_low],
                caseDataB_Windward[lowHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );
            double CpB_Windward_HighTheta = InterpolateCp(
                caseDataB_Windward[highHOverL][theta_low],
                caseDataB_Windward[highHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );

            double CpB_Leeward_LowTheta = InterpolateCp(
                caseDataB_Leeward[lowHOverL][theta_low],
                caseDataB_Leeward[lowHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );
            double CpB_Leeward_HighTheta = InterpolateCp(
                caseDataB_Leeward[highHOverL][theta_low],
                caseDataB_Leeward[highHOverL][theta_high],
                theta_low,
                theta_high,
                t_theta
            );

            // Step 2: Interpolate over h/L
            double CpA_Windward = InterpolateCp(CpA_Windward_LowTheta, CpA_Windward_HighTheta, lowHOverL, highHOverL, t_hOverL);
            double CpA_Leeward = InterpolateCp(CpA_Leeward_LowTheta, CpA_Leeward_HighTheta, lowHOverL, highHOverL, t_hOverL);
            double CpB_Windward = InterpolateCp(CpB_Windward_LowTheta, CpB_Windward_HighTheta, lowHOverL, highHOverL, t_hOverL);
            double CpB_Leeward = InterpolateCp(CpB_Leeward_LowTheta, CpB_Leeward_HighTheta, lowHOverL, highHOverL, t_hOverL);

            // Create and return RoofCpCases object using the new constructor
            return new RoofCpCases(CpA_Windward, CpB_Windward, CpA_Leeward, CpB_Leeward);
        }

        public static double InterpolateCp(double lowCp, double highCp, double lowTheta, double highTheta, double t_theta)
        {
            // Linear interpolation formula
            return lowCp + (highCp - lowCp) * t_theta;
        }

        public static double GetClosestHOverLRange(double hOverL)
        {
            // Define the possible ranges for h/L
            double[] hOverLRanges = { 0.25, 0.5, 1.0 };

            // Find the closest value to hOverL from the predefined ranges
            double closest = hOverLRanges.OrderBy(range => Math.Abs(range - hOverL)).First();
            return closest;
        }
    }

    public class RoofCpCases
    {
        public double Cp_CaseA_Windward;
        public double Cp_CaseB_Windward;
        public double Cp_CaseA_Leeward;
        public double Cp_CaseB_Leeward;

        public RoofCpCases(double interpolated_Cp_CaseA_Windward, double interpolated_Cp_CaseB_Windward, double interpolated_Cp_CaseA_Leeward, double interpolated_Cp_CaseB_Leeward)
        {
            this.Cp_CaseA_Windward = interpolated_Cp_CaseA_Windward;
            this.Cp_CaseB_Windward = interpolated_Cp_CaseB_Windward;
            this.Cp_CaseA_Leeward = interpolated_Cp_CaseA_Leeward;
            this.Cp_CaseB_Leeward = interpolated_Cp_CaseB_Leeward;
        }
    }
}
