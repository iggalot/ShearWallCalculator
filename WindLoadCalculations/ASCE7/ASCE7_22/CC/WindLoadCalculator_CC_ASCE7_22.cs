using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.ASCE7;
using ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator.WindLoadCalculations
{
    /// <summary>
    /// Computes C&C pressures for ASCE 7-16 using the analytical method of Part 3 on page 350
    /// </summary>
    public class WindLoadCalculator_CC_ASCE7_22: WindLoadCalculator_ASCE7_22_Base
    {
        public delegate bool TryGetGcpDelegate(int id, out double gcp);

        public override ASCE7_Versions ASCEVersion { get => ASCE7_Versions.ASCE_VER_7_22; }

        public WindLoadCalculator_CC_ASCE7_22(WindParameters_Base p, BuildingData bldg_data)
        {
            buildingData = bldg_data;
            Parameters = p;

            try
            {

                if (buildingData.MeanRoofHeight <= 60)
                {
                    CreateExtGcpCurves();
                }
                else
                {
                    throw new Exception("ERROR: Building max mean roof height has exceeded 60 ft -- " + buildingData.MeanRoofHeight + " ft.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CalculateRoofPressures(
            TryGetGcpDelegate tryGetGcp,
            Dictionary<int, PressureData> targetDict
            )
        {
            foreach (var areaEntry in RoofAreaCalculator.effWindAreas)
            {
                int id = areaEntry.Key;

                double gcp;
                if (!tryGetGcp(id, out gcp))
                    continue;

                var q_h = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);
                var kd = GetKd(Parameters.AnalysisType);
                var _gcpi = GetGCpi();
                
                targetDict.Add(id, new PressureData()
                {
                    AreaID = id,
                    qh = q_h,
                    GCp = gcp,
                    ExternalPressure = q_h * gcp * kd,
                    GCpi = _gcpi,
                    InternalPressure = q_h * _gcpi * kd,
                });
            }
        }

        private void CalculateWallPressures_BuildingLength(
            TryGetGcpDelegate tryGetGcp,
            Dictionary<int, PressureData> targetDict
            )
        {
            foreach (var areaEntry in WallAreaCalculator_BldgLength.effWindAreas)
            {
                int id = areaEntry.Key;

                double gcp;
                if (!tryGetGcp(id, out gcp))
                    continue;

                var q_h = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);
                var kd = GetKd(Parameters.AnalysisType);
                var _gcpi = GetGCpi();

                targetDict.Add(id, new PressureData()
                {
                    AreaID = id,
                    qh = q_h,
                    GCp = gcp,
                    ExternalPressure = q_h * gcp * kd,
                    GCpi = _gcpi,
                    InternalPressure = q_h * _gcpi * kd,
                });
            }
        }

        private void CalculateWallPressures_BuildingWidth(
            TryGetGcpDelegate tryGetGcp,
            Dictionary<int, PressureData> targetDict
            )
        {
            foreach (var areaEntry in WallAreaCalculator_BldgWidth.effWindAreas)
            {
                int id = areaEntry.Key;

                double gcp;
                if (!tryGetGcp(id, out gcp))
                    continue;

                var q_h = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);
                var kd = GetKd(Parameters.AnalysisType);
                var _gcpi = GetGCpi();

                targetDict.Add(id, new PressureData()
                {
                    AreaID = id,
                    qh = q_h,
                    GCp = gcp,
                    ExternalPressure = q_h * gcp * kd,
                    GCpi = _gcpi,
                    InternalPressure = q_h * _gcpi * kd,
                });
            }
        }

        public override void CalculateExternalPressures()
        {
            // Calculate the pressures andstore them in the appropriate dictionary.
            CalculateRoofPressures(TryGetGCp_Pos_Roof_ByAreaID, windPressureRoof_Pos_External);
            CalculateRoofPressures(TryGetGCp_Neg_Roof_ByAreaID, windPressureRoof_Neg_External);

            CalculateWallPressures_BuildingLength(TryGetGCp_Pos_BuildingLengthWall_ByAreaID, windPressureBuildingLengthWall_Pos_External);
            CalculateWallPressures_BuildingLength(TryGetGCp_Neg_BuildingLengthWall_ByAreaID, windPressureBuildingLengthWall_Neg_External);

            CalculateWallPressures_BuildingWidth(TryGetGCp_Pos_BuildingWidthWall_ByAreaID, windPressureBuildingWidthWall_Pos_External);
            CalculateWallPressures_BuildingWidth(TryGetGCp_Neg_BuildingWidthWall_ByAreaID, windPressureBuildingWidthWall_Neg_External);
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
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public virtual bool TryGetGCp_Pos_Roof_ByAreaID(int id, out double gcp)
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
        public virtual bool TryGetGCp_Neg_Roof_ByAreaID(int id, out double gcp)
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
        public virtual bool TryGetGCp_Pos_BuildingLengthWall_ByAreaID(int id, out double gcp)
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
        public virtual bool TryGetGCp_Neg_BuildingLengthWall_ByAreaID(int id, out double gcp)
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
        public virtual bool TryGetGCp_Pos_BuildingWidthWall_ByAreaID(int id, out double gcp)
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
        public virtual bool TryGetGCp_Neg_BuildingWidthWall_ByAreaID(int id, out double gcp)
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
