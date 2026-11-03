using System.Text.RegularExpressions;

namespace ShearWallCalculator.Design
{
    /// <summary>
    /// Which product picture a hold-down or strap shows in the catalog: one per product line, named by the model's leading
    /// letters ("HDUE5-SDS3" → HDUE, "DTT2Z" → DTT, "LTTP2" → LTTP, "ST6224" → ST). The pictures are local files
    /// (HoldownPictures\HDUE.png beside the program), cropped from the manufacturer's product photos; never read from the web.
    /// </summary>
    public static class HoldownPictures
    {
        private static readonly Regex Letters = new Regex("^[A-Z]+", RegexOptions.Compiled);

        /// <summary>The product line of <paramref name="model"/>: its leading capital letters; "" when it has none.</summary>
        public static string LineOf(string model) => model == null ? "" : Letters.Match(model.Trim().ToUpperInvariant()).Value;

        /// <summary>The picture's file name for <paramref name="model"/> ("HDUE.png"); null when the model has no line.</summary>
        public static string FileFor(string model)
        {
            string line = LineOf(model);
            return line.Length == 0 ? null : line + ".png";
        }
    }
}
