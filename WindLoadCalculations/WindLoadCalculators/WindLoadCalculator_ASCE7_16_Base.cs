namespace ShearWallCalculator.WindLoadCalculations.WindLoadCalculators
{
    /// <summary>
    ///  In ASCE7-16, the dynamic wind pressure coefficient includes Kd in the dynamic wind calculation.  
    ///  In ASCE7_22 it does not -- instead applying Kd to the external and net pressure calculations separately.  
    ///  In the end the calculations are the same.  This now better matches these codes.  
    ///  But dyanmic wind pressure qz and qh will be drastically different.
    /// </summary>
    public class WindLoadCalculator_ASCE7_16_Base : WindLoadCalculator_Base
    {
        public override double CalculateDynamicWindPressure(double z)
        {
            if (Parameters == null)
                return -1000;

            WindLoadParameters_Base p = Parameters;

            double V = p.WindSpeed;
            double Kd = p.Kd;
            double Kzt = p.Kzt;
            double I = p.ImportanceFactor;
            double Kz = GetKz(z, p.ExposureCategory);
            double qz = 0.00256 * Kz * Kzt * Kd * V * V * I;
            return qz;
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures
        /// </summary>
        public override void CalculateExternalPressures()
        {
            // roof pressure positive
            foreach (var area in Parameters.RoofAreaCalculator.effWindAreas)
            {
                if (TryGetGCp_Pos_Roof_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureRoof_Pos_External.Add(area.Key, pressure);
                }
            }

            // roof pressure negative
            foreach (var area in Parameters.RoofAreaCalculator.effWindAreas)
            {
                if (TryGetGCp_Neg_Roof_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureRoof_Neg_External.Add(area.Key, pressure);
                }
            }

            // BuildingWidth pressure positive
            foreach (var area in Parameters.WallAreaCalculator_BldgWidth.effWindAreas)
            {
                if (TryGetGCp_Pos_BuildingWidthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureBuildingWidthWall_Pos_External.Add(area.Key, pressure);
                }
            }

            // BuildingWidth pressure negative
            foreach (var area in Parameters.WallAreaCalculator_BldgWidth.effWindAreas)
            {
                if (TryGetGCp_Neg_BuildingWidthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureBuildingWidthWall_Neg_External.Add(area.Key, pressure);
                }
            }

            // BuildingLength pressure positive
            foreach (var area in Parameters.WallAreaCalculator_BldgLength.effWindAreas)
            {
                if (TryGetGCp_Pos_BuildingLengthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureBuildingLengthWall_Pos_External.Add(area.Key, pressure);
                }
            }

            // BuildingLength pressure negative
            foreach (var area in Parameters.WallAreaCalculator_BldgLength.effWindAreas)
            {
                if (TryGetGCp_Neg_BuildingLengthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureBuildingLengthWall_Neg_External.Add(area.Key, pressure);
                }
            }
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures minus the internal pressure qh * GCpi
        /// </summary>
        public override void CalculateNetPressures()
        {
            var kd = Parameters.Kd;
            // roof pressure positive
            foreach (var items in windPressureRoof_Pos_External)
            {
                double int_pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi() * kd;
                windPressureRoof_Pos_Net.Add(items.Key, items.Value + int_pressure);
            }

            // roof pressure negative
            foreach (var items in windPressureRoof_Neg_External)
            {
                double int_pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                windPressureRoof_Neg_Net.Add(items.Key, items.Value - int_pressure);
            }

            // BuildingWidth wall pressure positive
            foreach (var items in windPressureBuildingWidthWall_Pos_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                windPressureBuildingWidthWall_Pos_Net.Add(items.Key, items.Value + pressure);
            }

            // BuildingWidth wall pressure negative
            foreach (var items in windPressureBuildingWidthWall_Neg_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                windPressureBuildingWidthWall_Neg_Net.Add(items.Key, items.Value - pressure);
            }

            // BuildingLength pressure positive
            foreach (var items in windPressureBuildingLengthWall_Pos_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                windPressureBuildingLength_Pos_Net.Add(items.Key, items.Value + pressure);
            }

            // Building Length pressure negative
            foreach (var items in windPressureBuildingLengthWall_Neg_External)
            {
                double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                windPressureBuildingLengthWall_Neg_Net.Add(items.Key, items.Value - pressure);
            }
        }
    }
}
