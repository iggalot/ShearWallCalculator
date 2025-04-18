using System;
using System.Collections.Generic;
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



            WallResultsDataGrid.ItemsSource = null;
            WallResultsDataGrid.ItemsSource = wall_results;

            RoofResultsDataGrid.ItemsSource = null;
            RoofResultsDataGrid.ItemsSource = roof_results;
        }

        private static List<WindPressurResult_Wall> CalculateWallPressureResults(WindLoadParameters parameters, Dictionary<string, double> wall_zones)
        {
            List<WindPressurResult_Wall> wall_results = new List<WindPressurResult_Wall>();
            foreach (var kvp in wall_zones)
            {
                WindPressurResult_Wall wpr = new WindPressurResult_Wall();
                wpr.Surface = kvp.Key;

                if (kvp.Key == "Windward Wall - z=0ft")
                {
                    wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
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
                    wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
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
                    wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
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
                    wpr.Cp = WindLoadCalculator.GetCpLeewardWall(parameters);
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
                    wpr.End = parameters.BuildingLength / 2;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }
                else if (kvp.Key == "Windward Roof h/2->h")
                {
                    wpr.Start = parameters.BuildingLength / 2;
                    wpr.End = parameters.BuildingLength;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }
                else if (kvp.Key == "Windward Roof h->2h")
                {
                    wpr.Start = parameters.BuildingLength;
                    wpr.End = 2 * parameters.BuildingLength;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }
                else if (kvp.Key == "Windward Roof h->end")
                {
                    wpr.Start = parameters.BuildingLength;
                    wpr.End = parameters.BuildingLength * 2;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
                }

                else if (kvp.Key == "Leeward Roof 0->h/2")
                {
                    wpr.Start = 0;
                    wpr.End = parameters.BuildingLength / 2;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Leeward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Leeward, 2);
                }
                else if (kvp.Key == "Leeward Roof h/2->h")
                {
                    wpr.Start = parameters.BuildingLength / 2;
                    wpr.End = parameters.BuildingLength;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Leeward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Leeward, 2);
                }
                else if (kvp.Key == "Leeward Roof h->2h")
                {
                    wpr.Start = parameters.BuildingLength;
                    wpr.End = 2 * parameters.BuildingLength;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Leeward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Leeward, 2);
                }
                else if (kvp.Key == "Leeward Roof h->end")
                {
                    wpr.Start = parameters.BuildingLength;
                    wpr.End = parameters.BuildingLength * 2;
                    wpr.CpA = Math.Round(cases.Cp_CaseA_Leeward, 2);
                    wpr.CpB = Math.Round(cases.Cp_CaseB_Leeward, 2);

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
                        zones.Add("Windward Roof h->end", 0);
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
                    zones.Add("Leeward Roof 0->h/2", 0);

                }
                else if (L > 0.5 * h && L <= h)
                {
                    zones.Add("Windward Roof 0->h/2", 0);
                    zones.Add("Windward Roof h/2->h", 0);
                    zones.Add("Leeward Roof 0->h/2", 0);
                    zones.Add("Leeward Roof h/2->h", 0);
                }
                else if (L > h && L <= 2.0 * h)
                {
                    zones.Add("Windward Roof 0->h/2", 0);
                    zones.Add("Windward Roof h/2->h", 0);
                    zones.Add("Windward Roof h->2h", 0);
                    zones.Add("Leeward Roof 0->h/2", 0);
                    zones.Add("Leeward Roof h/2->h", 0);
                    zones.Add("Leeward Roof h->2h", 0);
                }
                else
                {
                    zones.Add("Windward Roof 0->h/2", 0);
                    zones.Add("Windward Roof h/2->h", 0);
                    zones.Add("Windward Roof h->2h", 0);
                    zones.Add("Windward Roof h->end", 0);
                    zones.Add("Leeward Roof 0->h/2", 0);
                    zones.Add("Leeward Roof h/2->h", 0);
                    zones.Add("Leeward Roof h->2h", 0);
                    zones.Add("Leeward Roof h->end", 0);
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

            Dictionary<string, double> RoofCoeff_hOVerL_0_5_CaseA_Windward = new Dictionary<string, double>();
            RoofCoeff_hOVerL_0_5_CaseA_Windward.Add("Windward Roof 0->h/2", -0.9);
            RoofCoeff_hOVerL_0_5_CaseA_Windward.Add("Windward Roof h/2->h", -0.9);
            RoofCoeff_hOVerL_0_5_CaseA_Windward.Add("Windward Roof h->2h", -0.5);
            RoofCoeff_hOVerL_0_5_CaseA_Windward.Add("Windward Roof h->end", -0.3);

            Dictionary<string, double> RoofCoeff_hOVerL_0_5_CaseB_Windward = new Dictionary<string, double>();
            RoofCoeff_hOVerL_0_5_CaseB_Windward.Add("Windward Roof 0->h/2", -0.18);
            RoofCoeff_hOVerL_0_5_CaseB_Windward.Add("Windward Roof h/2->h", -0.18);
            RoofCoeff_hOVerL_0_5_CaseB_Windward.Add("Windward Roof h->2h", -0.18);
            RoofCoeff_hOVerL_0_5_CaseB_Windward.Add("Windward Roof h->end", -0.18);

            Dictionary<string, double> RoofCoeff_hOVerL_1_0_CaseA_Windward = new Dictionary<string, double>();
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add("Windward Roof 0->h/2", -1.3);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add("Windward Roof h/2->h", -0.7);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add("Windward Roof h->2h", -0.7);
            RoofCoeff_hOVerL_1_0_CaseA_Windward.Add("Windward Roof h->end", -0.7);

            Dictionary<string, double> RoofCoeff_hOVerL_1_0_CaseB_Windward = new Dictionary<string, double>();
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add("Windward Roof 0->h/2", -0.18);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add("Windward Roof h/2->h", -0.18);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add("Windward Roof h->2h", -0.18);
            RoofCoeff_hOVerL_1_0_CaseB_Windward.Add("Windward Roof h->end", -0.18);

            double CpA, CpB;
            if (hOverL <= 0.5)
            {
                switch (zone_name)
                {
                    case "Windward Roof 0->h/2":
                        CpA = -0.9;
                        CpB = -0.18;
                        break;
                    case "Windward Roof h/2->h":
                        CpA = -0.9;
                        CpB = -0.18;
                        break;
                    case "Windward Roof h->2h":
                        CpA = -0.5;
                        CpB = -0.18;
                        break;

                    case "Windward Roof h->end":
                        CpA = -0.3;
                        CpB = -0.18;
                        break;
                    default:
                        CpA = 0.0;
                        CpB = 0.0;
                        break;
                }
            }
            else if (hOverL > 0.5 && hOverL <= 1.0)
            {
                // Interpolate for h/L
                double caseA, caseB;
                double t = (hOverL - 0.5) / (1.0 - 0.5);
                switch (zone_name)
                {
                    case "Windward Roof 0->h/2":
                        CpA = -0.9 +  (-0.4 * t);
                        CpB = -0.18;
                        break;
                    case "Windward Roof h/2->h":
                        CpA = -0.9 + (0.2 * t);
                        CpB = -0.18;
                        break;
                    case "Windward Roof h->2h":
                        CpA = -0.5 + (-0.2 * t);
                        CpB = -0.18;
                        break;
                    case "Windward Roof h->end":
                        CpA = -0.3 + (-0.4 * t);
                        CpB = -0.18;
                        break;
                    default:
                        CpA = 0.0;
                        CpB = 0.0;
                        break;
                }
            }
            else
            {
                switch (zone_name)
                {
                    case "Windward Roof 0->h/2":
                        CpA = -1.3;
                        CpB = -0.18;
                        break;
                    case "Windward Roof h/2->h":
                        CpA = -0.7;
                        CpB = -0.18;
                        break;
                    case "Windward Roof h->2h":
                        CpA = -0.7;
                        CpB = -0.18;
                        break;

                    case "Windward Roof h->end":
                        CpA = -0.7;
                        CpB = -0.18;
                        break;
                    default:
                        CpA = 0.0;
                        CpB = 0.0;
                        break;
                }
            }

            return new RoofCpCases(CpA, CpB, 0, 0); // 0 here because no leeward surface on flat roof
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
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(25, -0.6);
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(30, -0.6);
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(35, -0.6);
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(45, -0.6);
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(60, -0.6);
            RoofCoeff_hOVerL_0_25_CaseB_Leeward.Add(90, -0.6);


            Dictionary<double, double> RoofCoeff_hOVerL_0_50_CaseB_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(10, -0.5);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(15, -0.5);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(20, -0.6);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(25, -0.6);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(30, -0.6);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(35, -0.6);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(45, -0.6);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(60, -0.6);
            RoofCoeff_hOVerL_0_50_CaseB_Leeward.Add(90, -0.6);

            Dictionary<double, double> RoofCoeff_hOVerL_1_0_CaseB_Leeward = new Dictionary<double, double>();
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(10, -0.7);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(15, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(20, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(25, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(30, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(35, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(45, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(60, -0.6);
            RoofCoeff_hOVerL_1_0_CaseB_Leeward.Add(90, -0.6);


            Dictionary<double, Dictionary<double, double>> RoofCoefficientCp_CaseB_Leeward = new Dictionary<double, Dictionary<double, double>>();
            RoofCoefficientCp_CaseB_Leeward.Add(0.25, RoofCoeff_hOVerL_0_25_CaseB_Leeward);
            RoofCoefficientCp_CaseB_Leeward.Add(0.5, RoofCoeff_hOVerL_0_50_CaseB_Leeward);
            RoofCoefficientCp_CaseB_Leeward.Add(1.0, RoofCoeff_hOVerL_1_0_CaseB_Leeward);

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

            return new RoofCpCases(interpolated_Cp_CaseA_Windward, interpolated_Cp_CaseB_Windward, interpolated_Cp_CaseA_Leeward, interpolated_Cp_CaseB_Leeward);

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
