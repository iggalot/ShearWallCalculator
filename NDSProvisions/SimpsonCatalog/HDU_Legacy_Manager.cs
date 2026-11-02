using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    /// <summary>
    /// The discontinued HDU-SDS2.5 hold-downs, kept so designs that already specify them can still be checked. They are their
    /// own family (<c>HDU_Legacy_ToFoundation</c>); the designer never picks them unless that family is chosen for a wall.
    /// Sources:
    ///  - Simpson Strong-Tie, Wood Construction Connectors C-C-2021, p. 53, "HDU/DTT Holdowns (cont.)" table: every option
    ///    below (checked 2026-11-02). HDU is not in C-C-2026 (index p. 4; HDUE/DTT table p. 61).
    ///  - Simpson Strong-Tie engineering letter L-C-HDUECONV25, "HDUE™ Holdown One-for-One Replacement for Discontinued HDU",
    ///    April 2, 2025 (copy: docs/references/L-C-HDUECONV25.pdf): "Simpson Strong-Tie® is discontinuing the HDU holdown
    ///    series, replacing it with the HDUE series". Its Table 1 (maximum allowable loads) and Table 2 (W, H, B, CL, SO)
    ///    match the values below; the letter does not give the smaller-post options, which come from C-C-2021 p. 53 only.
    /// </summary>
    public class HDU_Legacy_Manager : BaseSimpsonConnectorManager<HDU_Data>
    {
        public Dictionary<string, HDU_Data> HDU_Dict = new Dictionary<string, HDU_Data>();

        public HDU_Legacy_Manager()
        {
            LoadHDUData();
        }

        private void LoadHDUData()
        {
            // HDU2-SDS2.5
            // DISCONTINUED. Simpson Strong-Tie C-C-2021 (Wood Construction Connectors), p. 53, "HDU/DTT Holdowns (cont.)" table, (160) loads; checked 2026-11-02. Max. option, W, H, B, CL, SO: also L-C-HDUECONV25 Tables 1 and 2
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
            // DISCONTINUED. Simpson Strong-Tie C-C-2021 (Wood Construction Connectors), p. 53, "HDU/DTT Holdowns (cont.)" table, (160) loads; checked 2026-11-02. Max. option, W, H, B, CL, SO: also L-C-HDUECONV25 Tables 1 and 2
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
            // DISCONTINUED. Simpson Strong-Tie C-C-2021 (Wood Construction Connectors), p. 53, "HDU/DTT Holdowns (cont.)" table, (160) loads; checked 2026-11-02. Max. option, W, H, B, CL, SO: also L-C-HDUECONV25 Tables 1 and 2
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
            // DISCONTINUED. Simpson Strong-Tie C-C-2021 (Wood Construction Connectors), p. 53, "HDU/DTT Holdowns (cont.)" table, (160) loads; checked 2026-11-02. Max. option, W, H, B, CL, SO: also L-C-HDUECONV25 Tables 1 and 2
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
            // DISCONTINUED. Simpson Strong-Tie C-C-2021 (Wood Construction Connectors), p. 53, "HDU/DTT Holdowns (cont.)" table, (160) loads; checked 2026-11-02. Max. option, W, H, B, CL, SO: also L-C-HDUECONV25 Tables 1 and 2
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
            // DISCONTINUED. Simpson Strong-Tie C-C-2021 (Wood Construction Connectors), p. 53, "HDU/DTT Holdowns (cont.)" table, (160) loads; checked 2026-11-02. Max. option, W, H, B, CL, SO: also L-C-HDUECONV25 Tables 1 and 2
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
                CodeRef = "IBC / FL / LA",  // C-C-2021 p. 53: the 3-1/2 x 5-1/2 option has no code listing ("—"); the others IBC, FL, LA
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



        public string PrintAllHDUData()
        {
            string str = "";
            foreach (KeyValuePair<string, HDU_Data> hdu in HDU_Dict)
            {
                str += hdu.Value.ToString() + "\n\n";
            }
            return str;
        }

        /// <summary>
        /// Returns a list of all HDU_Dict models with allowable tension load greater than the minimum load for the given wood type.
        /// </summary>
        /// <param name="minimumLoad">The minimum allowable tension load to filter by.</param>
        /// <param name="woodType">The wood type to filter by (e.g., WOODTYPE_DF_SP or WOODTYPE_SPF_HF).</param>
        /// <returns>A list of HDU_Dict models that exceed the minimum load for the specified wood type.</returns>

        public override List<HDU_Data> GetTypedModelsExceedingMinLoad(double reqLoad, WoodTypes woodType)

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
                        if (allowableTensionLoad >= reqLoad)
                        {
                            matchingModels.Add(hduData);
                            break; // No need to check further fasteners for this model
                        }
                    }
                }
            }

            return matchingModels;
        }

        public override List<Dictionary<string, BaseSimpsonConnectorData>> GetValidConnectors()
        {
            //List<HDU_Data> temp_list = Catalog.HDUManager.GetHDUModelsExceedingMinLoad(req_load, woodType);
            // Your existing logic
            return new List<Dictionary<string, BaseSimpsonConnectorData>>(); // Placeholder
        }
    }
}
