using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    /// <summary>
    /// LSTHD / STHD strap-tie hold-downs cast into the foundation, standard and rim-joist (RJ) models.
    /// Source: Simpson Strong-Tie, Wood Construction Connectors catalog C-C-2026, pp. 66–68, "LSTHD/STHD Strap-Tie Holdowns";
    /// loads from p. 67, table "Tension Loads for STHD Installations" (ASD, (160), DF/SP/SPF/HF, f'c ≥ 2,500 psi).
    /// The values are unchanged from C-C-2021 p. 59. Copy: docs/references/C-C-2026.pdf.
    /// The allowable load depends on the stemwall, the seismic row, cracked or uncracked concrete and where the strap is in
    /// the wall (midwall, corner, endwall); the design takes those from the project's <c>StrapHoldownConditions</c>.
    /// Not modelled (the engineer applies them): LSTHD8 0.9 × loads for 1"–4" spalls; two-pour downturn footings; brick ledge.
    /// </summary>
    public class STHD_Manager : BaseSimpsonConnectorManager<STHD_Data>
    {
        public Dictionary<string, STHD_Data> STHD_Dict = new Dictionary<string, STHD_Data>();

        public STHD_Manager()
        {
            LoadSTHDData();
        }

        private static STHD_Installation Row(double stemwall, SthdSeismic seismic, int nails, double[] uncracked, double[] cracked) =>
            new STHD_Installation { MinStemwall = stemwall, Seismic = seismic, Nails_Quantity = nails, Uncracked = uncracked, Cracked = cracked };

        private void LoadSTHDData()
        {
            const SthdSeismic wind = SthdSeismic.WindAndSdcAB, sdcCF = SthdSeismic.SdcCtoF;

            // C-C-2026 p. 67, "Tension Loads for STHD Installations": LSTHD8 / LSTHD8RJ, 14 ga (p. 66 "Material"),
            // L = 18-5/8" (RJ 32-1/8"), le = 8", nails 0.148 x 3-1/4; columns midwall, corner, endwall.
            // Deflection, table footnote 5: 0.089" on framing, 0.114" over 1/2" max. structural sheathing.
            var lsthd8 = new List<STHD_Installation>
            {
                Row(6, wind,  20, new double[] { 2985, 2590, 1620 }, new double[] { 2565, 2225, 1395 }),
                Row(8, wind,  20, new double[] { 2985, 2590, 2135 }, new double[] { 2565, 2225, 1835 }),
                Row(6, sdcCF, 16, new double[] { 2270, 2090, 1220 }, new double[] { 2250, 1950, 1220 }),
                Row(8, sdcCF, 16, new double[] { 2615, 2125, 1635 }, new double[] { 2250, 1950, 1610 }),
            };

            // C-C-2026 p. 67, same table: STHD10 / STHD10RJ, 12 ga, L = 24-5/8" (RJ 38-1/8"), le = 10".
            // Deflection, footnote 5: 0.117" on framing, 0.146" over sheathing.
            var sthd10 = new List<STHD_Installation>
            {
                Row(6, wind,  24, new double[] { 3535, 3535, 1960 }, new double[] { 2910, 2910, 1635 }),
                Row(8, wind,  28, new double[] { 4755, 4075, 3015 }, new double[] { 4020, 3350, 2480 }),
                Row(6, sdcCF, 18, new double[] { 2750, 2750, 1615 }, new double[] { 2550, 2550, 1435 }),
                Row(8, sdcCF, 20, new double[] { 3400, 2940, 2295 }, new double[] { 3400, 2940, 2175 }),
            };

            // C-C-2026 p. 67, same table: STHD14 / STHD14RJ, 12 ga, L = 26-1/8" (RJ 39-5/8"), le = 14".
            // Deflection, footnote 5: 0.118" on framing, 0.164" over sheathing.
            var sthd14 = new List<STHD_Installation>
            {
                Row(6, wind,  30, new double[] { 4935, 4935, 3065 }, new double[] { 4935, 4935, 3065 }),
                Row(8, wind,  30, new double[] { 5285, 5285, 4410 }, new double[] { 5285, 5285, 4410 }),
                Row(6, sdcCF, 22, new double[] { 3695, 3695, 2685 }, new double[] { 3695, 3695, 2685 }),
                Row(8, sdcCF, 24, new double[] { 3815, 3815, 3500 }, new double[] { 3815, 3815, 3500 }),
            };

            // Standard models first, then the RJ models (same loads): the selection keeps catalog order on ties, so the
            // shorter standard strap wins unless the engineer picks an RJ model from the standard list.
            Add("LSTHD8", 14, 18.625, 8, false, 0.089, 0.114, lsthd8);   // C-C-2026 p. 67
            Add("STHD10", 12, 24.625, 10, false, 0.117, 0.146, sthd10);  // C-C-2026 p. 67
            Add("STHD14", 12, 26.125, 14, false, 0.118, 0.164, sthd14);  // C-C-2026 p. 67
            Add("LSTHD8RJ", 14, 32.125, 8, true, 0.089, 0.114, lsthd8);  // C-C-2026 p. 67 (rim joist column)
            Add("STHD10RJ", 12, 38.125, 10, true, 0.117, 0.146, sthd10); // C-C-2026 p. 67 (rim joist column)
            Add("STHD14RJ", 12, 39.625, 14, true, 0.118, 0.164, sthd14); // C-C-2026 p. 67 (rim joist column)
        }

        private void Add(string model, int gauge, double length, double le, bool rimJoist, double deflFraming, double deflSheathing, List<STHD_Installation> rows)
        {
            STHD_Dict.Add(model, new STHD_Data
            {
                Model = model, Gauge = gauge, L = length, Embedment = le, RimJoist = rimJoist, CodeRef = "IBC / FL",
                DeflectionOnFraming = deflFraming, DeflectionOverSheathing = deflSheathing, Installations = rows,
            });
        }

        /// <summary>The minimum stemwall widths the table lists (each model's fastener options, in this order).</summary>
        public static readonly double[] Stemwalls = { 6, 8 };

        /// <summary>
        /// Models with an installation good for the load in the most conservative case the table gives for the seismic row
        /// "Wind and SDC A&amp;B": cracked concrete at an endwall (the design itself uses the project's conditions).
        /// </summary>
        public override List<STHD_Data> GetTypedModelsExceedingMinLoad(double reqLoad, WoodTypes woodType) =>
            STHD_Dict.Values.Where(m => m.Installations.Any(i => i.Seismic == SthdSeismic.WindAndSdcAB && i.Allowable(true, SthdPositions.Endwall) >= reqLoad)).ToList();

        public override List<Dictionary<string, BaseSimpsonConnectorData>> GetValidConnectors() =>
            new List<Dictionary<string, BaseSimpsonConnectorData>>();
    }
}
