using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.Helpers;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Windows.Controls;

namespace ShearWallVisualizer.Helpers
{
    public static class EffectiveWindAreaRenderer
    {
        /// <summary>
        /// Draws the effective wind areas on the given canvas using the provided roof area data.
        /// </summary>
        public static void Draw(
            Canvas canvas,
            AreaCalculator_Base areaCalculator,
            BuildingData buildingData,
            string debugLabel = null)
        {
            if (debugLabel != null)
                Console.WriteLine($"DrawEffectiveAreas: {debugLabel}");

            if (canvas == null || areaCalculator == null || buildingData == null)
                return;

            canvas.Children.Clear();

            double scale = Math.Min(
                canvas.ActualWidth / buildingData.BuildingWidth,
                canvas.ActualHeight / buildingData.BuildingLength);

            foreach (var area in areaCalculator.effWindAreas)
            {
                BuildingDrawer.DrawEffectiveWindArea(
                    canvas,
                    area.Value,
                    scale,
                    BuildingDrawer.GetColorForRegion(area.Value.Label_Short));
            }
        }
    }

}
