using System.Collections.Generic;
using System.Linq;

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
}
