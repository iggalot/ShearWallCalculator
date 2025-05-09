using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShearWallCalculator
{
    /// <summary>
    /// Class that holds HDU / DTT holdown connection data from table on page 53 of Simpson Catalog
    /// </summary>
    /// 
    public class HTT_FastenerConfig
    {
        // Fastener Info
        public int Screws_Quantity { get; set; }         // Number of SDS screws
        public string Screws_Type { get; set; }             // SDS screw type
        public string ScrewSize { get; set; }            // Screw diameter
        public double Length { get; set; }               // Screw length

        // Minimum Wood Member Size
        public double WoodMinHeight { get; set; }       // minimum width of member
        public double WoodMinWidth { get; set; }        // minimum height of member

        public double DeflectionAtAllowableLoad { get; set; }  // deflection at allowable load


        public Dictionary<string, double> AllowableTensionLoads { get; set; } = new Dictionary<string, double>();

        public override string ToString()
        {

            var loads = AllowableTensionLoads != null && AllowableTensionLoads.Count > 0
                ? string.Join(", ", AllowableTensionLoads.Select(kv => $"{kv.Key}: {kv.Value} lbs"))
                : "None";

            return $"Fastener Configuration:\n" +
                   $"- Screws: {Screws_Quantity}x {Screws_Type} ({ScrewSize}, {Length}\" long)\n" +
                   $"- Wood Min Size: {WoodMinWidth}\" wide x {WoodMinHeight}\" high\n" +
                   $"- Deflection at Allowable Load: {DeflectionAtAllowableLoad}\"\n" +
                   $"- Allowable Tension Loads: {loads}";
        }
    }

    public class HTT_Manager
    {
        Dictionary<string, HTT_Data> HTT = new Dictionary<string, HTT_Data>();

        public HTT_Manager()
        {
            LoadHDUData();
        }

        private void LoadHDUData()
        {
            // LTTP2
            HTT.Add("LTTP2", new HTT_Data
            {
                Model = "LTTP2",
                Gauge = 10,
                W = 2.5625,
                L = 14.9375,
                B = 2.25,  // assumed 2x CL
                CL = 1.125,
                SO = 0.4375,
                AnchorBoltDia = 0.75,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<HTT_FastenerConfig>
                {
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 15,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1845 },
                            { "SPF_HF", 1695 }
                        },
                        DeflectionAtAllowableLoad = 0.009,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 12,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2135 },
                            { "SPF_HF", 1965 }
                        },
                        DeflectionAtAllowableLoad = 0.011,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 12,
                        Screws_Type = "SD",
                        ScrewSize = "#9",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2570 },
                            { "SPF_HF", 2045 }
                        },
                        DeflectionAtAllowableLoad = 0.015,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 12,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2275 },
                            { "SPF_HF", 2230 }
                        },
                        DeflectionAtAllowableLoad = 0.013,
                    },
                }
            });

            // LTTI31
            HTT.Add("LTTI31", new HTT_Data
            {
                Model = "LTTI31",
                Gauge = 18,
                W = 3.75,
                L = 31,
                B = 2.75,
                CL = 1.375,
                SO = 0.25,
                AnchorBoltDia = 0.625,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<HTT_FastenerConfig>
                {
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 3.0,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1350 },
                            { "SPF_HF", 1160 }
                        },
                        DeflectionAtAllowableLoad = 0.193,
                    }
                }
            });

            // HTT4
            HTT.Add("HTT4", new HTT_Data
            {
                Model = "HTT4",
                Gauge = 11,
                W = 2.5,
                L = 12.375,
                B = 2,
                CL = 1.3125,
                SO = 0.4375,
                AnchorBoltDia = 0.625,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<HTT_FastenerConfig>
                {
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 3000 },
                            { "SPF_HF", 2580 }
                        },
                        DeflectionAtAllowableLoad = 0.090,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 3.0,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 3610 },
                            { "SPF_HF", 3105 }
                        },
                        DeflectionAtAllowableLoad = 0.086,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4235 },
                            { "SPF_HF", 3640 }
                        },
                        DeflectionAtAllowableLoad = 0.123,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "SD",
                        ScrewSize = "#10",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 5.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4455 },
                            { "SPF_HF", 3830 }
                        },
                        DeflectionAtAllowableLoad = 0.112,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "SD",
                        ScrewSize = "#10",
                        Length = 1.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4455 },
                            { "SPF_HF", 3830 }
                        },
                        DeflectionAtAllowableLoad = 0.112,
                    },
                }
            });

            // HTT5
            HTT.Add("HTT5", new HTT_Data
            {
                Model = "HTT5",
                Gauge = 11,
                W = 2.5,
                L = 16,
                B = 2,
                CL = 1.4375,
                SO = 0.4375,
                AnchorBoltDia = 0.625,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<HTT_FastenerConfig>
                {
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 3.0,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4350 },
                            { "SPF_HF", 3740 }
                        },
                        DeflectionAtAllowableLoad = 0.120,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 3.0,
                        WoodMinHeight = 3.0,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4670 },
                            { "SPF_HF", 4015 }
                        },
                        DeflectionAtAllowableLoad = 0.116,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        WoodMinHeight = 3.0,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5090 },
                            { "SPF_HF", 4375 }
                        },
                        DeflectionAtAllowableLoad = 0.135,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "SD",
                        ScrewSize = "#10",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 5.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4555 },
                            { "SPF_HF", 3915 }
                        },
                        DeflectionAtAllowableLoad = 0.114,
                    }
                }
            });

            // HTT5KT
            HTT.Add("HTT5KT", new HTT_Data
            {
                Model = "HTT5KT",
                Gauge = 11,
                W = 2.5,
                L = 16,
                B = 2,
                CL = 1.4375,
                SO = 0.4375,
                AnchorBoltDia = 0.625,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<HTT_FastenerConfig>
                {
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "SD",
                        ScrewSize = "#10",
                        Length = 2.5,
                        WoodMinHeight = 3.0,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5445 },
                            { "SPF_HF", 5360 }
                        },
                        DeflectionAtAllowableLoad = 0.103,
                    }
                }
            });

            // HTT5-3/4
            HTT.Add("HTT5-3/4", new HTT_Data
            {
                Model = "HTT5-3/4",
                Gauge = 11,
                W = 2.5,
                L = 16,
                B = 2,
                CL = 1.4375,
                SO = 0.4375,
                AnchorBoltDia = 0.75,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<HTT_FastenerConfig>
                {
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 5.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4065 },
                            { "SPF_HF", 3495 }
                        },
                        DeflectionAtAllowableLoad = 0.103,
                    },
                    new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5090 },
                            { "SPF_HF", 4375 }
                        },
                        DeflectionAtAllowableLoad = 0.121,
                    },
                                        new HTT_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "SD",
                        ScrewSize = "#10",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 7.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4830 },
                            { "SPF_HF", 4155 }
                        },
                        DeflectionAtAllowableLoad = 0.100,
                    },
                }
            });

        }
        /// <summary>
        /// Returns a list of all HDU models with allowable tension load greater than the minimum load for the given wood type.
        /// </summary>
        /// <param name="minimumLoad">The minimum allowable tension load to filter by.</param>
        /// <param name="woodType">The wood type to filter by (e.g., WOODTYPE_DF_SP or WOODTYPE_SPF_HF).</param>
        /// <returns>A list of HDU models that exceed the minimum load for the specified wood type.</returns>
        public List<string> GetHTTModelsExceedingMinLoad(double minimumLoad, WoodTypes woodType)
        {
            string wood_type_string = "";
            switch (woodType)
            {
                case WoodTypes.WOODTYPE_DF_SP:
                    wood_type_string = "DF_SP";
                    break;
                case WoodTypes.WOODTYPE_SPF_HF:
                    wood_type_string = "SPF_HF";
                    break;
                default:
                    throw new ArgumentException("Invalid wood type.");
            }
            List<string> matchingModels = new List<string>();

            foreach (KeyValuePair<string, HTT_Data> httEntry in HTT)
            {
                var httData = httEntry.Value;

                // Check each fastener option for the given wood type
                foreach (var fastener in httData.FastenerOptions)
                {
                    // Find the allowable tension load for the given wood type
                    if (fastener.AllowableTensionLoads.ContainsKey(wood_type_string))
                    {
                        double allowableTensionLoad = fastener.AllowableTensionLoads[wood_type_string];

                        // Check if the allowable load exceeds the minimum
                        if (allowableTensionLoad >= minimumLoad)
                        {
                            matchingModels.Add(httData.Model);
                            break; // No need to check further fasteners for this model
                        }
                    }
                }
            }

            return matchingModels;
        }

        public string PrintAllHTTData()
        {
            string str = "";
            foreach (KeyValuePair<string, HTT_Data> htt in HTT)
            {
                str += htt.Value.ToString() + "\n\n";
            }
            return str;
        }

    }

    public class HTT_Data
    {
        // Basic Identification
        public string Model { get; set; }                // e.g., "HDU2-SDS2.5"
        public int Gauge { get; set; }                   // e.g., 14 gauge

        // Dimensions (in inches)
        public double W { get; set; }                   // width of bracket
        public double L { get; set; }                   // overall height of bracket
        public double B { get; set; }                   // depth of bracket
        public double CL { get; set; }                  // distance from flat surface of bracket to centerline of anchor bolt
        public double SO { get; set; }                  // seat offset of anchor bolt -- height to surface that nut and washer bear on.
        public double AnchorBoltDia { get; set; }      // Anchor bolt diameter
        public List<HTT_FastenerConfig> FastenerOptions { get; set; } = new List<HTT_FastenerConfig>();
        // Other Notes or Conditions
        public string CodeRef { get; set; }               // CodeReference "IBC / FL / LA" etc.

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (FastenerOptions != null && FastenerOptions.Count > 0)
            {
                for (int i = 0; i < FastenerOptions.Count; i++)
                {
                    sb.AppendLine($"Model: {Model}");
                    sb.AppendLine($"Gauge: {Gauge} ga");
                    sb.AppendLine($"Dimensions (inches): W = {W}, L = {L}, B = {B}, CL = {CL}, SO = {SO}");
                    sb.AppendLine($"Anchor Bolt Diameter: {AnchorBoltDia}");
                    sb.AppendLine($"Code Reference: {CodeRef}");
                    sb.AppendLine("Fastener Options:");
                    sb.AppendLine(FastenerOptions[i].ToString());
                    sb.AppendLine("----------------------");

                }
            }
            else
            {
                sb.AppendLine($"Model: {Model}");
                sb.AppendLine($"Gauge: {Gauge} ga");
                sb.AppendLine($"Dimensions (inches): W = {W}, L = {L}, B = {B}, CL = {CL}, SO = {SO}");
                sb.AppendLine($"Anchor Bolt Diameter: {AnchorBoltDia}");
                sb.AppendLine($"Code Reference: {CodeRef}");
                sb.AppendLine("Fastener Options: None");
                sb.AppendLine("----------------------");

            }

            return sb.ToString();
        }
    }
}
