using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3;
using System;

namespace ShearWallCalculator.WindLoadCalculations
{
    /// <summary>
    /// Computes C&C pressures for ASCE 7-16 using the analytical method of Part 3 on page 350
    /// </summary>
    public class WindLoadCalculator_CC_ASCE7_22: WindLoadCalculator_ASCE7_22_Base
    {
        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_22; }

        public WindLoadCalculator_CC_ASCE7_22(WindParameters_Base p, BuildingData bldg_data)
        {
            buildingData = bldg_data;
            Parameters = p;

            if (buildingData.MeanRoofHeight <= 60)
            {
                CreateExtGcpCurves();
            }
        }

        public void CreateExtGcpCurves()
        {
            switch (ASCEVersion)
            {
                case ASCE7_Versions.ASCE_VER_7_16:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_16.CreateRoofFigure_ASCE7_16(buildingData);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_16();
                    break;
                case ASCE7_Versions.ASCE_VER_7_22:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(buildingData);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_22();
                    break;
                default:
                    throw new Exception("ERROR: Invalid ASCE Version: " + ASCEVersion + " in WindLoadCalculator_Base constructor.");
            }
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures
        /// </summary>
        public override void CalculateExternalPressures()
        {
            var kd = Parameters.Kd;

            // roof pressure positive
            foreach (var area in RoofAreaCalculator.effWindAreas)
            {
                if (TryGetGCp_Pos_Roof_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp * kd;
                    windPressureRoof_Pos_External.Add(area.Key, pressure);
                }
            }

            // roof pressure negative
            foreach (var area in RoofAreaCalculator.effWindAreas)
            {
                if (TryGetGCp_Neg_Roof_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp * kd;
                    windPressureRoof_Neg_External.Add(area.Key, pressure);
                }
            }

            // BuildingWidth pressure positive
            foreach (var area in WallAreaCalculator_BldgWidth.effWindAreas)
            {
                if (TryGetGCp_Pos_BuildingWidthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp * kd;
                    windPressureBuildingWidthWall_Pos_External.Add(area.Key, pressure);
                }
            }

            // BuildingWidth pressure negative
            foreach (var area in WallAreaCalculator_BldgWidth.effWindAreas)
            {
                if (TryGetGCp_Neg_BuildingWidthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp * kd;
                    windPressureBuildingWidthWall_Neg_External.Add(area.Key, pressure);
                }
            }

            // BuildingLength pressure positive
            foreach (var area in WallAreaCalculator_BldgLength.effWindAreas)
            {
                if (TryGetGCp_Pos_BuildingLengthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp * kd;
                    windPressureBuildingLengthWall_Pos_External.Add(area.Key, pressure);
                }
            }

            // BuildingLength pressure negative
            foreach (var area in WallAreaCalculator_BldgLength.effWindAreas)
            {
                if (TryGetGCp_Neg_BuildingLengthWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp * kd;
                    windPressureBuildingLengthWall_Neg_External.Add(area.Key, pressure);
                }
            }
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Pos_Roof_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in extGCpCurve_Roof.RoofCurves_Pos)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Neg_Roof_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in extGCpCurve_Roof.RoofCurves_Neg)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Pos_BuildingLengthWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgLength.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in extGCpCurve_Wall.WallCurves_Pos)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Neg_BuildingLengthWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgLength.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in extGCpCurve_Wall.WallCurves_Neg)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Pos_BuildingWidthWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgWidth.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in extGCpCurve_Wall.WallCurves_Pos)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Neg_BuildingWidthWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgWidth.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in extGCpCurve_Wall.WallCurves_Neg)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Overhang_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in extGCpCurve_Roof.OverhangCurves)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }


    }
}
