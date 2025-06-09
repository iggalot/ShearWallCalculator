using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShearWallCalculator
{
    /// <summary>
    /// Class that holds HDU_Dict / DTT holdown connection data from table on page 53 of Simpson Catalog
    /// </summary>
    /// 
    public class HDU_FastenerConfig
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

    public class HDU_Data : BaseSimpsonConnectorData
    {
        // Basic Identification
        public string Model { get; set; }                // e.g., "HDU2-SDS2.5"
        public int Gauge { get; set; }                   // e.g., 14 gauge

        // Dimensions (in inches)
        public double W { get; set; }                   // width of bracket
        public double H { get; set; }                   // overall height of bracket
        public double B { get; set; }                   // depth of bracket
        public double CL { get; set; }                  // distance from flat surface of bracket to centerline of anchor bolt
        public double SO { get; set; }                  // seat offset of anchor bolt -- height to surface that nut and washer bear on.
        public double AnchorBoltDia { get; set; }        // Anchor bolt diameter
        public List<HDU_FastenerConfig> FastenerOptions { get; set; } = new List<HDU_FastenerConfig>();
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
                    sb.AppendLine($"Dimensions (inches): W = {W}, H = {H}, B = {B}, CL = {CL}, SO = {SO}");
                    sb.AppendLine($"Anchor Bolt Diameter: {AnchorBoltDia}\"");
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
                sb.AppendLine($"Dimensions (inches): W = {W}, H = {H}, B = {B}, CL = {CL}, SO = {SO}");
                sb.AppendLine($"Anchor Bolt Diameter: {AnchorBoltDia}\"");
                sb.AppendLine($"Code Reference: {CodeRef}");
                sb.AppendLine("Fastener Options: None");
                sb.AppendLine("----------------------");

            }

            return sb.ToString();
        }
    }
}
