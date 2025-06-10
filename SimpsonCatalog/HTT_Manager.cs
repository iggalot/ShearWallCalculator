using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    public class HTT_Manager : BaseSimpsonConnectorManager<HTT_Data>
    {
        public Dictionary<string, HTT_Data> HTT_Dict = new Dictionary<string, HTT_Data>();

        public HTT_Manager()
        {
            LoadHDUData();
        }

        private void LoadHDUData()
        {
            // LTTP2
            HTT_Dict.Add("LTTP2", new HTT_Data
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
            HTT_Dict.Add("LTTI31", new HTT_Data
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
            HTT_Dict.Add("HTT4", new HTT_Data
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
            HTT_Dict.Add("HTT5", new HTT_Data
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
            HTT_Dict.Add("HTT5KT", new HTT_Data
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
            HTT_Dict.Add("HTT5-3/4", new HTT_Data
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


        public string PrintAllHTTData()
        {
            string str = "";
            foreach (KeyValuePair<string, HTT_Data> htt in HTT_Dict)
            {
                str += htt.Value.ToString() + "\n\n";
            }
            return str;
        }

        /// <summary>
        /// Returns a list of all HDU_Dict models with allowable tension load greater than the minimum load for the given wood type.
        /// </summary>
        /// <param name="minimumLoad">The minimum allowable tension load to filter by.</param>
        /// <param name="woodType">The wood type to filter by (e.g., WOODTYPE_DF_SP or WOODTYPE_SPF_HF).</param>
        /// <returns>A list of HDU_Dict models that exceed the minimum load for the specified wood type.</returns>

        public override List<HTT_Data> GetTypedModelsExceedingMinLoad(double reqLoad, WoodTypes woodType)
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
            List<HTT_Data> matchingModels = new List<HTT_Data>();

            foreach (KeyValuePair<string, HTT_Data> httEntry in HTT_Dict)
            {
                HTT_Data httData = httEntry.Value;

                // Check each fastener option for the given wood type
                foreach (var fastener in httData.FastenerOptions)
                {
                    // Find the allowable tension load for the given wood type
                    if (fastener.AllowableTensionLoads.ContainsKey(wood_type_string))
                    {
                        double allowableTensionLoad = fastener.AllowableTensionLoads[wood_type_string];

                        // Check if the allowable load exceeds the minimum
                        if (allowableTensionLoad >= reqLoad)
                        {
                            matchingModels.Add(httData);
                            break; // No need to check further fasteners for this model
                        }
                    }
                }
            }

            return matchingModels;
        }

        public override List<Dictionary<string, BaseSimpsonConnectorData>> GetValidConnectors()
        {
            //    List<HTT_Data> temp_list = Catalog.HTTManager.GetHTTModelsExceedingMinLoad(req_load, woodType);
            return new List<Dictionary<string, BaseSimpsonConnectorData>>();
        }
    }
}
