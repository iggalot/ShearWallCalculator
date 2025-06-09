using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    public class HDU_Manager
    {
        public Dictionary<string, HDU_Data> HDU_Dict = new Dictionary<string, HDU_Data>();

        public HDU_Manager()
        {
            LoadHDUData();
        }

        private void LoadHDUData()
        {
            // DTT1Z
            HDU_Dict.Add("DTT1Z", new HDU_Data
            {
                Model = "DTT1Z",
                Gauge = 14,
                W = 1.5,
                H = 7.125,
                B = 1.4375,
                CL = 0.75,
                SO = 0.1875,
                AnchorBoltDia = 0.375,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 6,
                        Screws_Type = "SD",
                        ScrewSize = "#9",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 840 },
                            { "SPF_HF", 840 }
                        },
                        DeflectionAtAllowableLoad = 0.17,

                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 6,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 910 },
                            { "SPF_HF", 640 }
                        },
                        DeflectionAtAllowableLoad = 0.167,

                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 8,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 910 },
                            { "SPF_HF", 850 }
                        },
                        DeflectionAtAllowableLoad = 0.167,
                    },

                }
            });

            // DTT2Z
            HDU_Dict.Add("DTT2Z", new HDU_Data
            {
                Model = "DTT2Z",
                Gauge = 14,
                W = 3.25,
                H = 6.9375,
                B = 1.625,
                CL = 0.8125,
                SO = 0.1875,
                AnchorBoltDia = 0.5,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 8,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 1.5,
                        WoodMinHeight = 1.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1825 },
                            { "SPF_HF", 1800 }
                        },
                        DeflectionAtAllowableLoad = 0.105,
                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 8,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 1.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2145 },
                            { "SPF_HF", 1835 }
                        },
                        DeflectionAtAllowableLoad = 0.128,

                    }
                }
            });

            // DTT2Z-SDS2.5
            HDU_Dict.Add("DTT2Z-SDS2.5", new HDU_Data
            {
                Model = "DTT2Z-SDS2.5",
                Gauge = 14,
                W = 3.25,
                H = 6.9375,
                B = 1.625,
                CL = 0.8125,
                SO = 0.1875,
                AnchorBoltDia = 0.5,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 8,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2145 },
                            { "SPF_HF", 2105 }
                        },
                        DeflectionAtAllowableLoad = 0.128,
                    },
                }
            });

            // HDU2-SDS2.5
            HDU_Dict.Add("HDU2-SDS2.5", new HDU_Data
            {
                Model = "HDU2-SDS2.5",
                Gauge = 14,
                W = 3,
                H = 8.6875,
                B = 3.25,
                CL = 1.3125,
                SO = 1.375,
                AnchorBoltDia = 0.625,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 6,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 3075 },
                            { "SPF_HF", 2215 }
                        },
                        DeflectionAtAllowableLoad = 0.088,
                    }
                },
            });

            // HDU4-SDS2.5
            HDU_Dict.Add("HDU4-SDS2.5", new HDU_Data
            {
                Model = "HDU4-SDS2.5",
                Gauge = 14,
                W = 3,
                H = 10.9375,
                B = 3.25,
                CL = 1.3125,
                SO = 1.375,
                AnchorBoltDia = 0.625,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 10,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4565 },
                            { "SPF_HF", 3285 }
                        },
                        DeflectionAtAllowableLoad = 0.114,
                    }
                },
            });

            // HDU5
            HDU_Dict.Add("HDU5-SDS2.5", new HDU_Data
            {
                Model = "HDU5-SDS2.5",
                Gauge = 14,
                W = 3,
                H = 13.1875,
                B = 3.25,
                CL = 1.3125,
                SO = 1.375,
                AnchorBoltDia = 0.625,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 14,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5645 },
                            { "SPF_HF", 4340 }
                        },
                        DeflectionAtAllowableLoad = 0.115,
                    }
                },
            });

            // HDU8-SDS2.5
            HDU_Dict.Add("HDU8-SDS2.5", new HDU_Data
            {
                Model = "HDU8-SDS2.5",
                Gauge = 10,
                W = 3,
                H = 16.625,
                B = 3.5,
                CL = 1.375,
                SO = 1.5,
                AnchorBoltDia = 0.875,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 20,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 6765 },
                            { "SPF_HF", 5820 }
                        },
                        DeflectionAtAllowableLoad = 0.11,
                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 20,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3.5,
                        WoodMinWidth = 3.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 6970 },
                            { "SPF_HF", 5995 }
                        },
                        DeflectionAtAllowableLoad = 0.116,
                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 20,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3.5,
                        WoodMinWidth = 4.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 7870 },
                            { "SPF_HF", 6580 }
                        },
                        DeflectionAtAllowableLoad = 0.113,
                    }
                },
            });

            // HDU11
            HDU_Dict.Add("HDU11-SDS2.5", new HDU_Data
            {
                Model = "HDU11-SDS2.5",
                Gauge = 10,
                W = 3.0,
                H = 22.25,
                B = 3.5,
                CL = 1.375,
                SO = 1.5,
                AnchorBoltDia = 1.0,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 30,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3.5,
                        WoodMinWidth = 5.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 9535 },
                            { "SPF_HF", 8030 }
                        },
                        DeflectionAtAllowableLoad = 0.137,
                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 30,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3.5,
                        WoodMinWidth = 7.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 11175 },
                            { "SPF_HF", 9610 }
                        },
                        DeflectionAtAllowableLoad = 0.137,
                    }
                },

            });

            // HDU14
            HDU_Dict.Add("HDU14-SDS2.5", new HDU_Data
            {
                Model = "HDU14-SDS2.5",
                Gauge = 7,
                W = 3,
                H = 25.6875,
                B = 3.5,
                CL = 1.5625,
                SO = 1.5625,
                AnchorBoltDia = 1.0,
                CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>
                {
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 36,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3.5,
                        WoodMinWidth = 5.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 10770 },
                            { "SPF_HF", 9260 }
                        },
                        DeflectionAtAllowableLoad = 0.122,
                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 36,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3.5,
                        WoodMinWidth = 7.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 14390 },
                            { "SPF_HF", 12375 }
                        },
                        DeflectionAtAllowableLoad = 0.177,
                    },
                    new HDU_FastenerConfig
                    {
                        Screws_Quantity = 36,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 2.5,
                        WoodMinHeight = 3.5,
                        WoodMinWidth = 5.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 14445 },
                            { "SPF_HF", 12425 }
                        },
                        DeflectionAtAllowableLoad = 0.172,
                    }
                },
            });
        }

        /// <summary>
        /// Returns a list of all HDU_Dict models with allowable tension load greater than the minimum load for the given wood type.
        /// </summary>
        /// <param name="minimumLoad">The minimum allowable tension load to filter by.</param>
        /// <param name="woodType">The wood type to filter by (e.g., WOODTYPE_DF_SP or WOODTYPE_SPF_HF).</param>
        /// <returns>A list of HDU_Dict models that exceed the minimum load for the specified wood type.</returns>
        public List<HDU_Data> GetHDUModelsExceedingMinLoad(double minimumLoad, WoodTypes woodType)
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
            List<HDU_Data> matchingModels = new List<HDU_Data>();

            foreach (KeyValuePair<string, HDU_Data> hduEntry in HDU_Dict)
            {
                HDU_Data hduData = hduEntry.Value;

                // Check each fastener option for the given wood type
                foreach (var fastener in hduData.FastenerOptions)
                {
                    // Find the allowable tension load for the given wood type
                    if (fastener.AllowableTensionLoads.ContainsKey(wood_type_string))
                    {
                        double allowableTensionLoad = fastener.AllowableTensionLoads[wood_type_string];

                        // Check if the allowable load exceeds the minimum
                        if (allowableTensionLoad >= minimumLoad)
                        {
                            matchingModels.Add(hduData);
                            break; // No need to check further fasteners for this model
                        }
                    }
                }
            }

            return matchingModels;
        }

        public string PrintAllHDUData()
        {
            string str = "";
            foreach (KeyValuePair<string, HDU_Data> hdu in HDU_Dict)
            {
                str += hdu.Value.ToString() + "\n\n";
            }
            return str;
        }

    }
}
