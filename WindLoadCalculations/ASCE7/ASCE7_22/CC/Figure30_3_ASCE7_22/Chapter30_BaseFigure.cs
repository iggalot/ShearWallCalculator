using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations.Chapter30
{
    public abstract class Chapter30_BaseFigure
    {
        public string ChartTitle { get; set; }
        public string ChartCriteria { get; set; }
        public Dictionary<string, ExternalGCpCurve> RoofCurves_Pos { get; set; }
        public Dictionary<string, ExternalGCpCurve> RoofCurves_Neg { get; set; }
        public Dictionary<string, ExternalGCpCurve> OverhangCurves { get; set; } = null;
        public Dictionary<string, ExternalGCpCurve> WallCurves_Pos { get; set; }
        public Dictionary<string, ExternalGCpCurve> WallCurves_Neg { get; set; }

        protected Chapter30_BaseFigure()
        {
            RoofCurves_Pos = new Dictionary<string, ExternalGCpCurve>();
            RoofCurves_Neg = new Dictionary<string, ExternalGCpCurve>();
            OverhangCurves = new Dictionary<string, ExternalGCpCurve>();
            WallCurves_Pos = new Dictionary<string, ExternalGCpCurve>();
            WallCurves_Neg = new Dictionary<string, ExternalGCpCurve>();
        }
    }
}
