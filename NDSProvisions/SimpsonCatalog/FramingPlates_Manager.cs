using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    /// <summary>One framing plate's allowable load in one direction with one set of fasteners (lb, ASD).</summary>
    public sealed class FramingPlate
    {
        public string Model { get; set; }
        /// <summary>The catalog's configuration number (its figure) and the fasteners, as tabulated.</summary>
        public int Configuration { get; set; }
        public int Fasteners { get; set; }
        public string Fastener { get; set; }
        /// <summary>The catalog's direction of load: G along the plates and the rim (shear), H across them.</summary>
        public string Direction { get; set; }
        /// <summary>The (160) column (wind) by species group, "DF_SP" and "SPF_HF".</summary>
        public Dictionary<string, double> Allowable160 { get; set; } = new Dictionary<string, double>();
        public string CodeRef { get; set; }
    }

    /// <summary>
    /// Framing plates that tie a rim or blocking to the wall's top plates. Source: Simpson Strong-Tie, Wood Construction
    /// Connectors C-C-2026, p. 310, "LTP4/LTP5/A34/A35 Framing Angles and Plates (cont.)", the (160) loads; configuration 7
    /// "LTP4 attaching Top Plates to Rim Board", 8 "LTP5 ... Attaching Plate to Rim Board". Footnote 3: installed over 3/8"
    /// wood structural panel sheathing with the tabulated fasteners, 0.72 of the listed load; over 1/2", 0.64; 0.131" x 2-1/2"
    /// nails or #9 x 2-1/2" SD screws achieve the full load in both. Copy: docs/references/C-C-2026.pdf.
    /// </summary>
    public sealed class FramingPlates_Manager
    {
        public const string Page = "p. 310";

        public static readonly string[] Citations =
        {
            "Simpson Strong-Tie C-C-2026, p. 310, \"LTP4/LTP5/A34/A35 Framing Angles and Plates (cont.)\": LTP4, configuration 7 (\"LTP4 attaching Top Plates to Rim Board\"), (12) 0.131 x 1-1/2 nails, load direction G (along the plates), (160): DF/SP 715 lb, SPF/HF 615 lb.",
            "Footnote 3: \"LTP4 and LTP5 can be installed over 3/8\" wood structural panel sheathing with tabulated fasteners and achieve 0.72 of the listed load, or over 1/2\" sheathing and achieve 0.64 of the listed load. 0.131\" x 2-1/2\" nails or #9 x 2-1/2\" SD screws will achieve full load in both applications.\"",
        };

        public List<FramingPlate> Plates { get; } = new List<FramingPlate>
        {
            new FramingPlate { Model = "LTP4", Configuration = 7, Fasteners = 12, Fastener = "0.131 x 1-1/2 nails", Direction = "G", Allowable160 = { ["DF_SP"] = 715, ["SPF_HF"] = 615 }, CodeRef = "IBC, FL, LA" },
            new FramingPlate { Model = "LTP4", Configuration = 7, Fasteners = 12, Fastener = "0.131 x 1-1/2 nails", Direction = "H", Allowable160 = { ["DF_SP"] = 525, ["SPF_HF"] = 450 }, CodeRef = "IBC, FL, LA" },
            new FramingPlate { Model = "LTP4", Configuration = 7, Fasteners = 12, Fastener = "#9 x 1-1/2 SD screws", Direction = "G", Allowable160 = { ["DF_SP"] = 910, ["SPF_HF"] = 780 }, CodeRef = "IBC, FL, LA" },
            new FramingPlate { Model = "LTP4", Configuration = 7, Fasteners = 12, Fastener = "#9 x 1-1/2 SD screws", Direction = "H", Allowable160 = { ["DF_SP"] = 800, ["SPF_HF"] = 690 }, CodeRef = "IBC, FL, LA" },
            new FramingPlate { Model = "LTP5", Configuration = 8, Fasteners = 12, Fastener = "0.131 x 1-1/2 nails", Direction = "G", Allowable160 = { ["DF_SP"] = 565, ["SPF_HF"] = 485 }, CodeRef = "IBC, FL, LA" },
            new FramingPlate { Model = "LTP5", Configuration = 8, Fasteners = 12, Fastener = "0.131 x 1-1/2 nails", Direction = "H", Allowable160 = { ["DF_SP"] = 465, ["SPF_HF"] = 400 }, CodeRef = "IBC, FL, LA" },
            new FramingPlate { Model = "LTP5", Configuration = 8, Fasteners = 12, Fastener = "#9 x 1-1/2 SD screws", Direction = "G", Allowable160 = { ["DF_SP"] = 800, ["SPF_HF"] = 685 } },
            new FramingPlate { Model = "LTP5", Configuration = 8, Fasteners = 12, Fastener = "#9 x 1-1/2 SD screws", Direction = "H", Allowable160 = { ["DF_SP"] = 730, ["SPF_HF"] = 625 } },
        };

        public FramingPlate Of(string model, string fastener, string direction) =>
            Plates.FirstOrDefault(p => p.Model == model && p.Fastener == fastener && p.Direction == direction);
    }
}
