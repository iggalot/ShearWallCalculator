using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;

namespace ShearWallVisualizer.Controls
{
    public partial class WindLoadResultsControl_CC : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs
        {
            public WindLoadParameters _parameters { get; }
            public List<WindPressureResult_Wall> _wall_results { get; }
            public List<WindPressureResult_Roof> _roof_results { get; }

            public OnWindCalculatedEventArgs(WindLoadParameters parameters, List<WindPressureResult_Wall> wall_results, List<WindPressureResult_Roof> roof_results)
            {
                _parameters = parameters;
                _wall_results = wall_results;
                _roof_results = roof_results;
            }
        }

        protected virtual void OnWindCalculated(WindLoadParameters parameters, List<WindPressureResult_Wall> wall_results, List<WindPressureResult_Roof> roof_results)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters, wall_results, roof_results));
        }

        private WindLoadParameters _parameters;

        public List<WindPressureResult_Wall> wall_results = new List<WindPressureResult_Wall>();
        public List<WindPressureResult_Roof> roof_results = new List<WindPressureResult_Roof>();

        public WindLoadResultsControl_CC()
        {
            
        }

        public WindLoadResultsControl_CC(WindLoadParameters parameters)
        {
            InitializeComponent();

            _parameters = parameters;

            this.Loaded += WindLoadResultsControl_CC_Loaded;
        }

        private void WindLoadResultsControl_CC_Loaded(object sender, RoutedEventArgs e)
        {
            //Dictionary<string, double> wall_zones = WindLoadCalculator.CalculateMWFRS_WallZones(_parameters);
            //Dictionary<string, double> roof_zones = WindLoadCalculator.CalculateMWFRS_RoofZones(_parameters);

            //// compute the wind load results tables
            //wall_results = CalculateWallPressureResults(_parameters, wall_zones);
            //roof_results = CalculateRoofPressureResults(_parameters, roof_zones);

            //tbl_qh.Text = Math.Round(WindLoadCalculator.CalculateDynamicWindPressure(_parameters, _parameters.BuildingHeight), 2).ToString();
            //tbl_theta.Text = Math.Round(_parameters.RoofPitch, 2).ToString();
            //tbl_hOverL.Text = Math.Round(_parameters.BuildingHeight / _parameters.BuildingLength, 2).ToString();
            //tbl_h.Text = Math.Round(_parameters.BuildingHeight, 2).ToString();
            //tbl_windOrientation.Text = _parameters.RidgeDirection;

            //// Display wall results in the DataGrids
            //WallResultsDataGrid.ItemsSource = null;
            //WallResultsDataGrid.ItemsSource = wall_results;

            //RoofResultsDataGrid.ItemsSource = null;
            //RoofResultsDataGrid.ItemsSource = roof_results;

            //spResultsAndCanvas.Children.Add(new WindLoadGraphicCanvas(_parameters, wall_results, roof_results));

            //OnWindCalculated(_parameters, wall_results, roof_results);
        }

        private static List<WindPressureResult_Wall> CalculateWallPressureResults(WindLoadParameters parameters, Dictionary<string, double> wall_zones)
        {
            List<WindPressureResult_Wall> wall_results = new List<WindPressureResult_Wall>();

            //// Helper method to calculate common values for pressure calculation
            //double CalculatePressure(double qz, double qh, double Cp, double GCpi, double GustFactor)
            //{
            //    return  Math.Round(qz * GustFactor * Cp + GCpi * qh, 2);
            //}

            //// Loop through wall zones and calculate pressures
            //foreach (var kvp in wall_zones)
            //{
            //    WindPressureResult_Wall wpr = new WindPressureResult_Wall();
            //    wpr.Surface = kvp.Key;

            //    // Get the correct Cp value based on the surface type
            //    wpr.GCpi_A = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification); // suction case
            //    wpr.GCpi_B = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification); // internal balloon case

            //    switch (kvp.Key)
            //    {
            //        case "Windward Wall - z=0ft":
            //            wpr.z = 0;
            //            wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
            //            break;
            //        case "Windward Wall - z=15ft":
            //            wpr.z = 15;
            //            wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
            //            break;
            //        case "Windward Wall - z=h":
            //            wpr.z = parameters.BuildingHeight;
            //            wpr.Cp = WindLoadCalculator.GetCpWindwardwall(parameters);
            //            break;

            //        case "Leeward Wall":
            //            // swap these values since the balloon case is additive for leeward and sidewalls
            //            wpr.z = parameters.BuildingHeight;
            //            wpr.Cp = WindLoadCalculator.GetCpLeewardWall(parameters);
            //            break;

            //        case "Sidewall":
            //            // swap these values since the balloon case is additive for leeward and sidewalls
            //            wpr.z = parameters.BuildingHeight;
            //            wpr.Cp = WindLoadCalculator.GetCpSidewall(parameters);
            //            break;

            //        default:
            //            wpr.GCpi_A = 0;
            //            wpr.GCpi_B = 0;
            //            wpr.z = 0;
            //            wpr.Cp = 0; // Default value, can be adjusted based on use case
            //            break;
            //    }

            //    // Common values
            //    wpr.Kz = Math.Round(WindLoadCalculator.GetKz(wpr.z, parameters.ExposureCategory), 2);
            //    var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
            //    wpr.qz = Math.Round(0.00256 * wpr.Kz * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);
            //    var qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);

            //    // Base pressure without internal effects
            //    wpr.PressBaseA = Math.Round(CalculatePressure(wpr.qz, qh, wpr.Cp, 0, parameters.GustFactor), 2);
            //    // Calculate pressures using common logic
            //    wpr.Suction1 = Math.Round(CalculatePressure(wpr.qz, qh, wpr.Cp, wpr.GCpi_A, parameters.GustFactor), 2);
            //    wpr.Balloon1 = Math.Round(CalculatePressure(wpr.qz, qh, wpr.Cp, wpr.GCpi_B, parameters.GustFactor), 2);

            //    // Add the result to the list
            //    wall_results.Add(wpr);
            //}

            return wall_results;
        }

        private static List<WindPressureResult_Roof> CalculateRoofPressureResults(WindLoadParameters parameters, Dictionary<string, double> roof_zones)
        {
            List<WindPressureResult_Roof> roof_results = new List<WindPressureResult_Roof>();
            //foreach (var kvp in roof_zones)
            //{
            //    WindPressureResult_Roof wpr = new WindPressureResult_Roof();
            //    wpr.Surface = kvp.Key;

            //    RoofCpCases cases;
            //    if(parameters.RidgeDirection == "Perpendicular to Wind")
            //    {
            //        if(parameters.RoofPitch >= 10.0)
            //        {
            //           cases = WindLoadCalculator.CalculateRoofCp_PerpendicularRidge(parameters.BuildingHeight, parameters.BuildingLength, parameters.RoofPitch);
            //        } else
            //        {
            //            cases = WindLoadCalculator.CalculateRoofCp_ForFlatRoofOrParallelRidge(parameters.BuildingHeight, parameters.BuildingLength, kvp.Key);
            //        }
            //    } else
            //    {
            //        cases = WindLoadCalculator.CalculateRoofCp_ForFlatRoofOrParallelRidge(parameters.BuildingHeight, parameters.BuildingLength, kvp.Key);
            //    }

            //    if (kvp.Key == "Windward Roof 0->h/2")
            //    {
            //        wpr.Start = 0;
            //        wpr.End = parameters.BuildingHeight / 2;
            //        wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
            //        wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
            //    }
            //    else if (kvp.Key == "Windward Roof h/2->h")
            //    {
            //        wpr.Start = parameters.BuildingHeight / 2;
            //        wpr.End = parameters.BuildingHeight;
            //        wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
            //        wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
            //    }
            //    else if (kvp.Key == "Windward Roof h->2h")
            //    {
            //        wpr.Start = parameters.BuildingHeight;
            //        wpr.End = 2 * parameters.BuildingHeight;
            //        wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
            //        wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
            //    }
            //    else if (kvp.Key == "Windward Roof 2h->end")
            //    {
            //        wpr.Start = 2 * parameters.BuildingHeight;
            //        wpr.End = parameters.BuildingLength;
            //        wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
            //        wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
            //    }

            //    else if (kvp.Key == "Windward Roof")
            //    {
            //        wpr.Start = 0.0;
            //        wpr.End = parameters.BuildingLength / 2;
            //        wpr.CpA = Math.Round(cases.Cp_CaseA_Windward, 2);
            //        wpr.CpB = Math.Round(cases.Cp_CaseB_Windward, 2);
            //    }
            //    else if (kvp.Key == "Leeward Roof")
            //    {
            //        wpr.Start = parameters.BuildingLength / 2;
            //        wpr.End = parameters.BuildingLength;
            //        wpr.CpA = Math.Round(cases.Cp_CaseA_Leeward, 2);
            //        wpr.CpB = Math.Round(cases.Cp_CaseB_Leeward, 2);
            //    }
            //     else
            //    {
            //        wpr.Start = 0;
            //        wpr.End = 0;
            //        wpr.CpA = 0;
            //        wpr.CpB = 0;
            //    }

            //    // Adjust the end point if it's off the building length
            //    if (wpr.End > parameters.BuildingLength)
            //    {
            //        wpr.End = parameters.BuildingLength;
            //    }

            //    wpr.GCpi_A = +1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
            //    wpr.GCpi_B = -1.0 * WindLoadCalculator.GetGCpiMagnitude(parameters.EnclosureClassification);
            //    var Kzh = Math.Round(WindLoadCalculator.GetKz(parameters.BuildingHeight, parameters.ExposureCategory), 2);
            //    wpr.qh = Math.Round(0.00256 * Kzh * parameters.Kzt * parameters.Kd * parameters.WindSpeed * parameters.WindSpeed * parameters.ImportanceFactor, 2);

            //    wpr.PressBaseA = Math.Round(wpr.qh * parameters.GustFactor * wpr.CpA, 2);
            //    wpr.PressBaseB = Math.Round(wpr.qh * parameters.GustFactor * wpr.CpB, 2);
            //    wpr.Suction1 = Math.Round(wpr.PressBaseA + wpr.GCpi_A * wpr.qh, 2);
            //    wpr.Balloon1 = Math.Round(wpr.PressBaseA - wpr.GCpi_A * wpr.qh, 2);
            //    wpr.Balloon2 = Math.Round(wpr.PressBaseB + wpr.GCpi_B * wpr.qh, 2);
            //    wpr.Suction2 = Math.Round(wpr.PressBaseB - wpr.GCpi_B * wpr.qh, 2);
            //    wpr.theta = parameters.RoofPitch;

            //    roof_results.Add(wpr);
            //}

            return roof_results;
        }
    }

    // Wind Load Calculator class
    public static class WindLoadCalculator_CC
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

        /// <summary>
        /// Calculates the dyanmic wind pressure q at a specified height z
        /// </summary>
        /// <param name="p"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static double CalculateDynamicWindPressure(WindLoadParameters p, double z)
        {
            double V = p.WindSpeed;
            double Kd = p.Kd;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * Kd * V * V * I;
            return qz;
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
}
