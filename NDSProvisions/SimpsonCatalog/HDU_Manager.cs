using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    /// <summary>
    /// HDUE / DTT hold-downs anchored to the foundation.
    /// Source: Simpson Strong-Tie, Wood Construction Connectors C-C-2026, pp. 60–61, "HDUE/DTT Holdowns"; values from the
    /// p. 61 table "Holdowns (cont.)" (ASD, (160)). DTT also pp. 326–327 (deck tension ties). Copy: docs/references/C-C-2026.pdf.
    /// C-C-2026 replaces the HDU-SDS2.5 line (HDU2 … HDU14, C-C-2021 p. 53) with the HDUE line; the HDU models are no longer listed.
    /// Minimum wood member is "thickness x width" (the first figure is the post dimension along the wall).
    /// Table footnotes not modelled: 3 (some loads exceed ASTM F1554 Grade 36 anchor bolts: the designer specifies the anchor),
    /// 4 (HDUE13 needs the heavy-hex nut supplied), 5 (alternate shorter screws: letter L-C-HDUESDS).
    /// </summary>
    public class HDU_Manager : BaseSimpsonConnectorManager<HDU_Data>
    {
        public Dictionary<string, HDU_Data> HDU_Dict = new Dictionary<string, HDU_Data>();

        public HDU_Manager()
        {
            LoadHDUData();
        }

        private static HDU_FastenerConfig Opt(int qty, string type, string size, double length, double thick, double width, double df, double spf, double defl) =>
            new HDU_FastenerConfig
            {
                Screws_Quantity = qty, Screws_Type = type, ScrewSize = size, Length = length, WoodMinHeight = thick, WoodMinWidth = width,
                AllowableTensionLoads = new Dictionary<string, double> { { "DF_SP", df }, { "SPF_HF", spf } },
                DeflectionAtAllowableLoad = defl,
            };

        private void Add(string model, int gauge, double w, double h, double b, double cl, double so, double bolt, params HDU_FastenerConfig[] options) =>
            HDU_Dict.Add(model, new HDU_Data
            {
                Model = model, Gauge = gauge, W = w, H = h, B = b, CL = cl, SO = so, AnchorBoltDia = bolt, CodeRef = "IBC / FL / LA",
                FastenerOptions = new List<HDU_FastenerConfig>(options),
            });

        private void LoadHDUData()
        {
            // Simpson Strong-Tie C-C-2026 (Wood Construction Connectors), p. 61, "HDUE/DTT Holdowns (cont.)" table, (160) loads
            // DTT1Z: min. member 1-1/2 x 3-1/2 in C-C-2026 (C-C-2021 p. 53 gave 1-1/2 x 5-1/2)
            Add("DTT1Z", 14, 1.5, 7.125, 1.4375, 0.75, 0.1875, 0.375,
                Opt(6, "SD", "#9", 1.5, 1.5, 3.5, 840, 840, 0.170),
                Opt(6, "", "0.148", 1.5, 1.5, 3.5, 910, 640, 0.167),
                Opt(8, "", "0.148", 1.5, 1.5, 3.5, 910, 850, 0.167));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table
            Add("DTT2Z", 14, 3.25, 6.9375, 1.625, 0.8125, 0.1875, 0.5,
                Opt(8, "SDS", "1/4", 1.5, 1.5, 3.5, 1825, 1800, 0.105),
                Opt(8, "SDS", "1/4", 1.5, 3, 3.5, 2145, 1835, 0.128));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table
            Add("DTT2Z-SDS2.5", 14, 3.25, 6.9375, 1.625, 0.8125, 0.1875, 0.5,
                Opt(8, "SDS", "1/4", 2.5, 3, 3.5, 2145, 2105, 0.128));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table (new model in C-C-2026)
            Add("HDUE3-SDS3", 14, 2.875, 8.6875, 3.375, 1.375, 1.5, 0.625,
                Opt(7, "SDS", "1/4", 3, 3, 3.5, 3790, 3340, 0.127));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table (new model in C-C-2026)
            Add("HDUE5-SDS3", 14, 2.875, 11.9375, 3.375, 1.375, 1.5, 0.625,
                Opt(10, "SDS", "1/4", 3, 3, 3.5, 5375, 4700, 0.146));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table (new model in C-C-2026; DF/SP 7,015 carries footnote 3)
            Add("HDUE7-SDS3", 14, 2.875, 14.6875, 3.375, 1.375, 1.5, 0.625,
                Opt(13, "SDS", "1/4", 3, 3, 3.5, 7015, 6030, 0.154));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table (new model in C-C-2026)
            Add("HDUE9-SDS3.5", 12, 3, 17.375, 4.3125, 1.4375, 1.5, 0.875,
                Opt(16, "SDS", "1/4", 3.5, 3.5, 3.5, 8425, 7305, 0.159),
                Opt(16, "SDS", "1/4", 3.5, 4.5, 3.5, 9390, 7995, 0.134));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table (new model in C-C-2026; heavy-hex nut, footnote 4)
            Add("HDUE13-SDS3.5", 12, 3, 23.0625, 4.3125, 1.4375, 1.5, 1.0,
                Opt(23, "SDS", "1/4", 3.5, 5.5, 3.5, 11900, 10215, 0.164),
                Opt(23, "SDS", "1/4", 3.5, 7.25, 3.5, 12950, 11030, 0.145),
                Opt(23, "SDS", "1/4", 3.5, 5.5, 5.5, 13110, 10980, 0.135));

            // Simpson Strong-Tie C-C-2026, p. 61, "HDUE/DTT Holdowns (cont.)" table (new model in C-C-2026; DF/SP 17,685 carries footnote 3)
            Add("HDUE17-SDS4.5", 10, 3, 27.875, 5.1875, 1.9375, 1.5, 1.0,
                Opt(28, "SDS", "1/4", 4.5, 5.5, 3.5, 16040, 13545, 0.094),
                Opt(28, "SDS", "1/4", 4.5, 5.5, 5.5, 17685, 14775, 0.111));
        }

        public string PrintAllHDUData()
        {
            string str = "";
            foreach (KeyValuePair<string, HDU_Data> hdu in HDU_Dict)
                str += hdu.Value.ToString() + "\n\n";
            return str;
        }

        /// <summary>All models with an option whose allowable tension for the wood type is at least the load.</summary>
        public override List<HDU_Data> GetTypedModelsExceedingMinLoad(double reqLoad, WoodTypes woodType)
        {
            string key;
            switch (woodType)
            {
                case WoodTypes.WOODTYPE_DF_SP: key = "DF_SP"; break;
                case WoodTypes.WOODTYPE_SPF_HF: key = "SPF_HF"; break;
                default: throw new ArgumentException("Invalid wood type.");
            }
            var matching = new List<HDU_Data>();
            foreach (var m in HDU_Dict.Values)
                foreach (var f in m.FastenerOptions)
                    if (f.AllowableTensionLoads.TryGetValue(key, out double a) && a >= reqLoad) { matching.Add(m); break; }
            return matching;
        }

        public override List<Dictionary<string, BaseSimpsonConnectorData>> GetValidConnectors() =>
            new List<Dictionary<string, BaseSimpsonConnectorData>>();
    }
}
