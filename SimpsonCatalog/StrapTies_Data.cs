using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShearWallCalculator
{
    /// <summary>
    /// Class that holds HDU_Dict / DTT holdown connection data from table on page 53 of Simpson Catalog
    /// </summary>
    /// 
    public class StrapTies_FastenerConfig : BaseSimpsonConnectorData
    {
        // Fastener Info
        public int Screws_Quantity { get; set; }         // Number of SDS screws
        public string Screws_Type { get; set; }             // SDS screw type
        public string ScrewSize { get; set; }            // Screw diameter
        public double Length { get; set; }               // Screw length

        public Dictionary<string, double> AllowableTensionLoads { get; set; } = new Dictionary<string, double>();

        public override string ToString()
        {

            var loads = AllowableTensionLoads != null && AllowableTensionLoads.Count > 0
                ? string.Join(", ", AllowableTensionLoads.Select(kv => $"{kv.Key}: {kv.Value} lbs"))
                : "None";

            return $"Fastener Configuration:\n" +
                   $"- Screws: {Screws_Quantity}x {Screws_Type} ({ScrewSize}, {Length}\" long)\n" +
                   $"- Allowable Tension Loads: {loads}";
        }
    }

    public class StrapTies_Data
    {
        // Basic Identification
        public string Model { get; set; }                // e.g., "HDU2-SDS2.5"
        public int Gauge { get; set; }                   // e.g., 14 gauge

        // Dimensions (in inches)
        public double W { get; set; }                   // width of bracket
        public double L { get; set; }                   // overall height of bracket
        public List<StrapTies_FastenerConfig> FastenerOptions { get; set; } = new List<StrapTies_FastenerConfig>();
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
                    sb.AppendLine($"Dimensions (inches): W = {W}, L = {L}");
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
                sb.AppendLine($"Dimensions (inches): W = {W}, L = {L}");
                sb.AppendLine($"Code Reference: {CodeRef}");
                sb.AppendLine("Fastener Options: None");
                sb.AppendLine("----------------------");

            }

            return sb.ToString();
        }
    }
}
