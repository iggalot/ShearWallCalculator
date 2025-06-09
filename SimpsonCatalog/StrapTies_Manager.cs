using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    public class StrapTies_Manager
    {
        Dictionary<string, StrapTies_Data> StrapTies_Dict = new Dictionary<string, StrapTies_Data>();

        public StrapTies_Manager()
        {
            LoadStrapTieData();
        }

        private void LoadStrapTieData()
        {
            // ST2115
            StrapTies_Dict.Add("ST2115", new StrapTies_Data
            {
                Model = "ST2115",
                Gauge = 20,
                W = 0.75,
                L = 16.3125,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 10,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 660 },
                            { "SPF_HF", 660 }
                        },
                    }

                }
            });

            // LSTA9
            StrapTies_Dict.Add("LSTA9", new StrapTies_Data
            {
                Model = "LSTA9",
                Gauge = 20,
                W = 1.25,
                L = 9,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 8,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 740 },
                            { "SPF_HF", 635 }
                        },
                    }

                }
            });

            // LSTA12
            StrapTies_Dict.Add("LSTA12", new StrapTies_Data
            {
                Model = "LSTA12",
                Gauge = 20,
                W = 1.25,
                L = 12,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 10,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 925 },
                            { "SPF_HF", 795 }
                        },
                    }

                }
            });

            // LSTA15
            StrapTies_Dict.Add("LSTA15", new StrapTies_Data
            {
                Model = "LSTA15",
                Gauge = 20,
                W = 1.25,
                L = 15,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 12,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1110 },
                            { "SPF_HF", 955 }
                        },
                    }

                }
            });

            // LSTA18
            StrapTies_Dict.Add("LSTA18", new StrapTies_Data
            {
                Model = "LSTA18",
                Gauge = 20,
                W = 1.25,
                L = 18,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 14,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1235 },
                            { "SPF_HF", 1115 }
                        },
                    }

                }
            });

            // LSTA21
            StrapTies_Dict.Add("LSTA21", new StrapTies_Data
            {
                Model = "LSTA21",
                Gauge = 20,
                W = 1.25,
                L = 21,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 16,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1235 },
                            { "SPF_HF", 1235 }
                        },
                    }

                }
            });

            // LSTA24
            StrapTies_Dict.Add("LSTA24", new StrapTies_Data
            {
                Model = "LSTA24",
                Gauge = 20,
                W = 1.25,
                L = 24,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1235 },
                            { "SPF_HF", 1235 }
                        },
                    }

                }
            });

            // LSTA30
            StrapTies_Dict.Add("LSTA30", new StrapTies_Data
            {
                Model = "LSTA30",
                Gauge = 18,
                W = 1.25,
                L = 30,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 22,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1640 },
                            { "SPF_HF", 1640 }
                        },
                    }

                }
            });

            // LSTA36
            StrapTies_Dict.Add("LSTA36", new StrapTies_Data
            {
                Model = "LSTA36",
                Gauge = 18,
                W = 1.25,
                L = 36,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 24,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1640 },
                            { "SPF_HF", 1640 }
                        },
                    }

                }
            });

            // MSTA9
            StrapTies_Dict.Add("MSTA9", new StrapTies_Data
            {
                Model = "MSTA9",
                Gauge = 18,
                W = 1.25,
                L = 9,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 8,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 750 },
                            { "SPF_HF", 650 }
                        },
                    }

                }
            });

            // MSTA12
            StrapTies_Dict.Add("MSTA12", new StrapTies_Data
            {
                Model = "MSTA12",
                Gauge = 18,
                W = 1.25,
                L = 12,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 10,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 940 },
                            { "SPF_HF", 810 }
                        },
                    }

                }
            });

            // MSTA15
            StrapTies_Dict.Add("MSTA15", new StrapTies_Data
            {
                Model = "MSTA15",
                Gauge = 18,
                W = 1.25,
                L = 15,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 12,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1130 },
                            { "SPF_HF", 970 }
                        },
                    }

                }
            });

            // MSTA18
            StrapTies_Dict.Add("MSTA18", new StrapTies_Data
            {
                Model = "MSTA18",
                Gauge = 18,
                W = 1.25,
                L = 18,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 14,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1315 },
                            { "SPF_HF", 1135 }
                        },
                    }

                }
            });

            // MSTA21
            StrapTies_Dict.Add("MSTA21", new StrapTies_Data
            {
                Model = "MSTA21",
                Gauge = 18,
                W = 1.25,
                L = 21,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 16,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1505 },
                            { "SPF_HF", 1295 }
                        },
                    }
                }
            });

            // MSTA24
            StrapTies_Dict.Add("MSTA24", new StrapTies_Data
            {
                Model = "MSTA24",
                Gauge = 18,
                W = 1.25,
                L = 24,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1640 },
                            { "SPF_HF", 1460 }
                        },
                    }
                }
            });

            // MSTA30
            StrapTies_Dict.Add("MSTA30", new StrapTies_Data
            {
                Model = "MSTA30",
                Gauge = 16,
                W = 1.25,
                L = 30,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 22,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2050 },
                            { "SPF_HF", 1825 }
                        },
                    }
                }
            });

            // MSTA36
            StrapTies_Dict.Add("MSTA36", new StrapTies_Data
            {
                Model = "MSTA36",
                Gauge = 16,
                W = 1.25,
                L = 36,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2050 },
                            { "SPF_HF", 2050 }
                        },
                    }
                }
            });

            // MSTA49
            StrapTies_Dict.Add("MSTA49", new StrapTies_Data
            {
                Model = "MSTA49",
                Gauge = 16,
                W = 1.25,
                L = 49,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2020 },
                            { "SPF_HF", 2020 }
                        },
                    }
                }
            });

            // ST9
            StrapTies_Dict.Add("ST9", new StrapTies_Data
            {
                Model = "ST9",
                Gauge = 16,
                W = 1.25,
                L = 9,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 8,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 885 },
                            { "SPF_HF", 765 }
                        },
                    }
                }
            });

            // ST12
            StrapTies_Dict.Add("ST12", new StrapTies_Data
            {
                Model = "ST12",
                Gauge = 16,
                W = 1.25,
                L = 11.625,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 10,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1105 },
                            { "SPF_HF", 955 }
                        },
                    }
                }
            });

            // ST18
            StrapTies_Dict.Add("ST18", new StrapTies_Data
            {
                Model = "ST18",
                Gauge = 16,
                W = 1.25,
                L = 17.75,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 14,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1420 },
                            { "SPF_HF", 1335 }
                        },
                    }
                }
            });

            // ST22
            StrapTies_Dict.Add("ST22", new StrapTies_Data
            {
                Model = "ST22",
                Gauge = 16,
                W = 1.25,
                L = 21.625,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 18,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1420 },
                            { "SPF_HF", 1420 }
                        },
                    }
                }
            });

            // HRS6
            StrapTies_Dict.Add("HRS6", new StrapTies_Data
            {
                Model = "HRS6",
                Gauge = 12,
                W = 1.375,
                L = 6,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 6,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 605 },
                            { "SPF_HF", 530 }
                        },
                    }
                }
            });

            // HRS8
            StrapTies_Dict.Add("HRS8", new StrapTies_Data
            {
                Model = "HRS8",
                Gauge = 12,
                W = 1.375,
                L = 8,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 10,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1010 },
                            { "SPF_HF", 880 }
                        },
                    }
                }
            });

            // HRS8
            StrapTies_Dict.Add("HRS12", new StrapTies_Data
            {
                Model = "HRS12",
                Gauge = 12,
                W = 1.375,
                L = 12,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 14,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1415 },
                            { "SPF_HF", 1230 }
                        },
                    }
                }
            });

            // ST292
            StrapTies_Dict.Add("ST292", new StrapTies_Data
            {
                Model = "ST292",
                Gauge = 20,
                W = 2.0625,
                L = 9.3125,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 12,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1260 },
                            { "SPF_HF", 1120 }
                        },
                    }
                }
            });

            // ST2122
            StrapTies_Dict.Add("ST2122", new StrapTies_Data
            {
                Model = "ST2122",
                Gauge = 20,
                W = 2.0625,
                L = 12.8125,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 16,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1530 },
                            { "SPF_HF", 1510 }
                        },
                    }
                }
            });

            // ST2215
            StrapTies_Dict.Add("ST2215", new StrapTies_Data
            {
                Model = "ST2215",
                Gauge = 20,
                W = 2.0625,
                L = 16.3125,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 20,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 1875 },
                            { "SPF_HF", 1875 }
                        },
                    }
                }
            });

            // ST6215
            StrapTies_Dict.Add("ST6215", new StrapTies_Data
            {
                Model = "ST6215",
                Gauge = 16,
                W = 2.0625,
                L = 16.3125,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 20,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2090 },
                            { "SPF_HF", 1910 }
                        },
                    }
                }
            });

            // ST6224
            StrapTies_Dict.Add("ST6224", new StrapTies_Data
            {
                Model = "ST6224",
                Gauge = 16,
                W = 2.0625,
                L = 23.3125,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 28,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2535 },
                            { "SPF_HF", 2535 }
                        },
                    }
                }
            });

            // ST6236
            StrapTies_Dict.Add("ST6236", new StrapTies_Data
            {
                Model = "ST6236",
                Gauge = 14,
                W = 2.0625,
                L = 33.6875,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 40,
                        Screws_Type = "",
                        ScrewSize = "0.162",
                        Length = 2.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 3845 },
                            { "SPF_HF", 3845 }
                        },
                    }
                }
            });

            // MSTI26
            StrapTies_Dict.Add("MSTI26", new StrapTies_Data
            {
                Model = "MSTI26",
                Gauge = 12,
                W = 2.0625,
                L = 26,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 26,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2745 },
                            { "SPF_HF", 2380 }
                        },
                    }
                }
            });

            // MSTI36
            StrapTies_Dict.Add("MSTI36", new StrapTies_Data
            {
                Model = "MSTI36",
                Gauge = 12,
                W = 2.0625,
                L = 26,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 36,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 3800 },
                            { "SPF_HF", 3295 }
                        },
                    }
                }
            });

            // MSTI48
            StrapTies_Dict.Add("MSTI48", new StrapTies_Data
            {
                Model = "MSTI48",
                Gauge = 12,
                W = 2.0625,
                L = 48,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 48,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5070 },
                            { "SPF_HF", 4390 }
                        },
                    }
                }
            });

            // MSTI60
            StrapTies_Dict.Add("MSTI60", new StrapTies_Data
            {
                Model = "MSTI60",
                Gauge = 12,
                W = 2.0625,
                L = 60,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 60,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5070 },
                            { "SPF_HF", 5070 }
                        },
                    }
                }
            });

            // MSTI72
            StrapTies_Dict.Add("MSTI72", new StrapTies_Data
            {
                Model = "MSTI72",
                Gauge = 12,
                W = 2.0625,
                L = 72,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 72,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5070 },
                            { "SPF_HF", 5070 }
                        },
                    }
                }
            });

            // HTP37Z
            StrapTies_Dict.Add("HTP37Z", new StrapTies_Data
            {
                Model = "HTP37Z",
                Gauge = 16,
                W = 3,
                L = 7,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 20,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 900 },
                            { "SPF_HF", 690 }
                        },
                    }
                }
            });

            // MSTC28
            StrapTies_Dict.Add("MSTC28", new StrapTies_Data
            {
                Model = "MSTC28",
                Gauge = 16,
                W = 3,
                L = 28.25,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 36,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 3.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 3460 },
                            { "SPF_HF", 2990 }
                        },
                    }
                }
            });

            // MSTC40
            StrapTies_Dict.Add("MSTC40", new StrapTies_Data
            {
                Model = "MSTC40",
                Gauge = 16,
                W = 3,
                L = 40.25,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 52,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 3.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4735 },
                            { "SPF_HF", 4315 }
                        },
                    }
                }
            });

            // MSTC52
            StrapTies_Dict.Add("MSTC52", new StrapTies_Data
            {
                Model = "MSTC52",
                Gauge = 16,
                W = 3,
                L = 52.25,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 62,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 3.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4735 },
                            { "SPF_HF", 4315 }
                        },
                    }
                }
            });

            // MSTC66
            StrapTies_Dict.Add("MSTC66", new StrapTies_Data
            {
                Model = "MSTC66",
                Gauge = 14,
                W = 3,
                L = 65.75,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 76,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 3.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5850 },
                            { "SPF_HF", 5850 }
                        },
                    }
                }
            });

            // MSTC78
            StrapTies_Dict.Add("MSTC78", new StrapTies_Data
            {
                Model = "MSTC78",
                Gauge = 14,
                W = 3,
                L = 77.75,
                CodeRef = "IBC / FL / LA",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 76,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 3.25,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 5850 },
                            { "SPF_HF", 5850 }
                        },
                    }
                }
            });

            // HRS416Z
            StrapTies_Dict.Add("HRS416Z", new StrapTies_Data
            {
                Model = "HRS416Z",
                Gauge = 12,
                W = 3.25,
                L = 16,
                CodeRef = " --- ",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 16,
                        Screws_Type = "SDS",
                        ScrewSize = "1/4",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2835 },
                            { "SPF_HF", 2305 }
                        },
                    }
                }
            });

            // LSTI49
            StrapTies_Dict.Add("LSTI49", new StrapTies_Data
            {
                Model = "LSTI49",
                Gauge = 18,
                W = 3.75,
                L = 49,
                CodeRef = " --- ",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 32,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 2970 },
                            { "SPF_HF", 2560 }
                        },
                    }
                }
            });

            // LSTI73
            StrapTies_Dict.Add("LSTI73", new StrapTies_Data
            {
                Model = "LSTI73",
                Gauge = 18,
                W = 3.75,
                L = 73,
                CodeRef = " --- ",

                FastenerOptions = new List<StrapTies_FastenerConfig>
                {
                    new StrapTies_FastenerConfig
                    {
                        Screws_Quantity = 48,
                        Screws_Type = "",
                        ScrewSize = "0.148",
                        Length = 1.5,
                        AllowableTensionLoads = new Dictionary<string, double>
                        {
                            { "DF_SP", 4205 },
                            { "SPF_HF", 3840 }
                        },
                    }
                }
            });
        }

        /// <summary>
        /// Returns a list of all HDU_Dict models with allowable tension load greater than the minimum load for the given wood type.
        /// </summary>
        /// <param name="req_load">The minimum allowable tension load to filter by.</param>
        /// <param name="woodType">The wood type to filter by (e.g., WOODTYPE_DF_SP or WOODTYPE_SPF_HF).</param>
        /// <returns>A list of HDU_Dict models that exceed the minimum load for the specified wood type.</returns>
        public List<StrapTies_Data> GetStrapTieModelsExceedingMinLoad(double req_load, WoodTypes woodType)
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
            List<StrapTies_Data> matchingModels = new List<StrapTies_Data>();

            foreach (KeyValuePair<string, StrapTies_Data> strapEntry in StrapTies_Dict)
            {
                StrapTies_Data strapData = strapEntry.Value;

                // Check each fastener option for the given wood type
                foreach (var fastener in strapData.FastenerOptions)
                {
                    // Find the allowable tension load for the given wood type
                    if (fastener.AllowableTensionLoads.ContainsKey(wood_type_string))
                    {
                        double allowableTensionLoad = fastener.AllowableTensionLoads[wood_type_string];

                        // Check if the allowable load exceeds the minimum
                        if (allowableTensionLoad >= req_load)
                        {
                            matchingModels.Add(strapData);
                            break; // No need to check further fasteners for this model
                        }
                    }
                }
            }

            return matchingModels;
        }



        public string PrintAllStrapTieData()
        {
            string str = "";
            foreach (KeyValuePair<string, StrapTies_Data> hdu in StrapTies_Dict)
            {
                str += hdu.Value.ToString() + "\n\n";
            }
            return str;
        }

    }
}
