using System.Collections.Generic;
using System.Text;

namespace ShearWallCalculator
{
    public class HTT_Data : BaseSimpsonConnectorData
    {
        // Basic Identification
        public int Gauge { get; set; }                   // e.g., 14 gauge

        // Dimensions (in inches)
        public double W { get; set; }                   // width of bracket
        public double L { get; set; }                   // overall height of bracket
        public double B { get; set; }                   // depth of bracket
        public double CL { get; set; }                  // distance from flat surface of bracket to centerline of anchor bolt
        public double SO { get; set; }                  // seat offset of anchor bolt -- height to surface that nut and washer bear on.
        public double AnchorBoltDia { get; set; }      // Anchor bolt diameter
        public List<HTT_FastenerConfig> FastenerOptions { get; set; } = new List<HTT_FastenerConfig>();
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
                    sb.AppendLine($"Dimensions (inches): W = {W}, L = {L}, B = {B}, CL = {CL}, SO = {SO}");
                    sb.AppendLine($"Anchor Bolt Diameter: {AnchorBoltDia}");
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
                sb.AppendLine($"Dimensions (inches): W = {W}, L = {L}, B = {B}, CL = {CL}, SO = {SO}");
                sb.AppendLine($"Anchor Bolt Diameter: {AnchorBoltDia}");
                sb.AppendLine($"Code Reference: {CodeRef}");
                sb.AppendLine("Fastener Options: None");
                sb.AppendLine("----------------------");

            }

            return sb.ToString();
        }
    }
}
