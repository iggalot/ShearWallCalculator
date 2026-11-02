using System.Collections.Generic;

namespace ShearWallCalculator
{
    /// <summary>Where the strap is cast in the stemwall (the catalog's load columns).</summary>
    public enum SthdPositions
    {
        Midwall = 0,  // 1.5 le or more from the end of the concrete
        Corner = 1,   // at an outside corner of the foundation
        Endwall = 2,  // at the end of a stemwall (lowest loads)
    }

    /// <summary>The seismic rows of the catalog table.</summary>
    public enum SthdSeismic
    {
        WindAndSdcAB = 0, // "Wind and SDC A&B" (also detached one- and two-family dwellings in SDC C, table footnote 7)
        SdcCtoF = 1,      // "SDC C–F"
    }

    /// <summary>
    /// One row of the STHD table: a model in a minimum stemwall width for one seismic row, with its required nails and the
    /// allowable tension (lb, ASD, loads apply to DF/SP/SPF/HF) for each position, in uncracked and cracked concrete.
    /// </summary>
    public sealed class STHD_Installation
    {
        public double MinStemwall { get; set; }          // in.
        public SthdSeismic Seismic { get; set; }
        public int Nails_Quantity { get; set; }
        public string NailSize { get; set; } = "0.148";
        public double NailLength { get; set; } = 3.25;

        /// <summary>Midwall, corner, endwall (index = <see cref="SthdPositions"/>).</summary>
        public double[] Uncracked { get; set; }
        public double[] Cracked { get; set; }

        public double Allowable(bool cracked, SthdPositions position) => (cracked ? Cracked : Uncracked)[(int)position];
    }

    /// <summary>An LSTHD / STHD strap-tie hold-down cast into the foundation (standard or rim-joist model).</summary>
    public class STHD_Data : BaseSimpsonConnectorData
    {
        public int Gauge { get; set; }
        public double W { get; set; } = 3;               // strap width (in.)
        public double L { get; set; }                    // strap length (in.)
        public double Embedment { get; set; }            // le (in.)
        public bool RimJoist { get; set; }               // RJ model: longer strap, up to a 17" clear span over the rim
        public string CodeRef { get; set; }

        /// <summary>Minimum wood member: double 2x studs (catalog: "the pattern allows for nailing to the edges of double 2x's").</summary>
        public double WoodMinHeight { get; set; } = 3;
        public double WoodMinWidth { get; set; } = 3.5;

        /// <summary>Deflection at the highest allowable load, installed on the framing (table footnote 5).</summary>
        public double DeflectionOnFraming { get; set; }
        /// <summary>Deflection at the highest allowable load, installed over 1/2" max. structural sheathing (table footnote 5).</summary>
        public double DeflectionOverSheathing { get; set; }

        public List<STHD_Installation> Installations { get; set; } = new List<STHD_Installation>();

        public override string ToString() => $"{Model} ({Gauge} ga, L = {L}\", le = {Embedment}\")";
    }
}
