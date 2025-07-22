using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30;
using ShearWallCalculator.WindLoadCalculations.Chapter30.Figure30_3;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Packaging;

namespace ShearWallCalculator.WindLoadCalculations
{
    public enum RoofTypes
    {
        ROOF_TYPE_FLAT = 0,
        ROOF_TYPE_GABLE = 1,
        ROOF_TYPE_HIP = 2
    }
    public enum ASCE7_Versions
    {
        ASCE_VER_7_16 = 0,  // this must start at index 0
        ASCE_VER_7_22 = 1
    }

    public enum WindLoadCalculationTypes
    {
        COMPONENT_AND_CLADDING = 0,  // Components and cladding  -- Chapter 30
        MWFRS = 1 // MWFRS
    }

    public enum WindExposureCategories
    {
        WIND_EXP_CAT_B = 0,
        WIND_EXP_CAT_C = 1,
        WIND_EXP_CAT_D = 2
    }

    public enum WindLoadCases
    {
        WLC_BaseA,
        WLC_BaseB,
        WLC_Balloon1,
        WLC_Balloon2,
        WLC_Suction1,
        WLC_Suction2
    }

    public enum WindZones_Walls_MWFRS
    {
        [Description("Windward Wall - z=0ft")]
        MWFRS_WW_0 = 0,
        [Description("Windward Wall - z=15ft")]
        MWFRS_WW_15 = 1,
        [Description("Windward Wall - z=h")]
        MWFRS_WW_h = 2,
        [Description("Leeward Wall")]
        MWFRS_LW_h = 3,
        [Description("Sidewall")]
        MWFRS_SW_h = 4
    }

    public enum WindZones_Roof_MWFRS
    {
        [Description("Windward Roof 0->h/2")]
        MWFRS_WR_0_h2 = 0,
        [Description("Windward Roof h/2->h")]
        MWFRS_WR_h2_h = 1,
        [Description("Windward Roof h->2h")]
        MWFRS_WR_h_2h = 2,
        [Description("Windward Roof > 2h")]
        MWFRS_WR_2h_L = 3,
        [Description("Windward Roof Full")]
        MWFRS_WR_Full = 4,
        [Description("Leeward Roof Full")]
        MWFRS_LR_Full = 5
    }

    public abstract class WindLoadCalculator_Base
    {
        public virtual ASCE7_Versions ASCEVersion { get; }
        public WindLoadParameters_Base Parameters { get; set; }
        public BuildingData buildingData { get; set; }

        // Which figure of Ch30_3_2A thru I to use for CC roof
        public Chapter30_BaseFigure extGCpCurve_Roof { get; set; }

        // The curve of Ch30_3_1 to use for CC walls
        public Chapter30_BaseFigure extGCpCurve_Wall { get; set; }

        public Dictionary<int, double> windPressureRoof_Pos_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureRoof_Neg_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureSideWall_Pos_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureSideWall_Neg_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureEndWall_Pos_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureEndWall_Neg_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureOverhang_External { get; set; } = new Dictionary<int, double>();

        public Dictionary<int, double> windPressureRoof_Pos_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureRoof_Neg_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureSideWall_Pos_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureSideWall_Neg_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureEndWall_Pos_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureEndWall_Neg_Net { get; set; } = new Dictionary<int, double>();

        public void CreateExtGcpCurves()
        {
            switch (ASCEVersion)
            {
                case ASCE7_Versions.ASCE_VER_7_16:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_16.CreateRoofFigure_ASCE7_16(
                         buildingData.RoofType, buildingData.MeanRoofHeight, buildingData.BuildingWidth, buildingData.RoofPitch);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_16();
                    break;
                case ASCE7_Versions.ASCE_VER_7_22:
                    extGCpCurve_Roof = Chapter30RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(
                        buildingData.RoofType, buildingData.MeanRoofHeight, buildingData.BuildingWidth, buildingData.RoofPitch);
                    extGCpCurve_Wall = new Figure30_3_1_ASCE7_22();
                    break;
                default:
                    throw new Exception("ERROR: Invalid ASCE Version: " + ASCEVersion + " in WindLoadCalculator_Base constructor.");
            }
        }

        /// <summary>
        /// Calculates the dyanmic wind pressure q at a specified height z per ASCE7_16 and ASCE7_22
        /// </summary>
        /// <param name="p"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public virtual double CalculateDynamicWindPressure(double z)
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


        // Get Kz approximation based on building height and exposure category
        public virtual double GetKz(double z, WindExposureCategories exposure)
        {
            double zg, alpha;

            switch (exposure)
            {
                case WindExposureCategories.WIND_EXP_CAT_B:
                    zg = 1200.0;
                    alpha = 7.0;
                    break;
                case WindExposureCategories.WIND_EXP_CAT_C:
                    zg = 900.0;
                    alpha = 9.5;
                    break;
                case WindExposureCategories.WIND_EXP_CAT_D:
                    zg = 700.0;
                    alpha = 11.5;
                    break;
                default:
                    zg = 900.0;
                    alpha = 9.5;
                    break;
            }

            z = Math.Max(z, 15); // Minimum height for Kz is 15 ft
            return 2.01 * Math.Pow(z / zg, 2.0 / alpha);
        }

        /// <summary>
        /// Compute Kd coefficient for the specific version of ASCE7
        /// </summary>
        /// <param name="calc_type"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual double GetKd(WindLoadCalculationTypes calc_type)
        {
            switch (calc_type)
            {
                case WindLoadCalculationTypes.COMPONENT_AND_CLADDING:
                    return 0.85;
                case WindLoadCalculationTypes.MWFRS:
                    return 0.85;
                default:
                    throw new NotImplementedException("ERROR:  " + calc_type + " not supported. ");
            }
        }

        /// <summary>
        /// The +/- coefficient for internal pressure coefficient GCpi from ASCE7_16 & ASCE7-22 Table 26.13-1
        /// </summary>
        /// <returns></returns>
        public virtual double GetGCpi()
        {
            switch (buildingData.EnclosureType)
            {
                case BuildingEnclosures.BLDG_ENCLOSED: return 0.18;
                case BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED: return 0.55;
                case BuildingEnclosures.BLDG_PARTIALLY_OPEN: return 0.18;
                case BuildingEnclosures.BLDG_OPEN: return 0.0;
                default: throw new Exception("ERROR: Invalid enclosure type: " + buildingData.EnclosureType + " in WindLoadCalculator_MWFRS_ASCE7_22 constructor.");
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


            if (!Parameters.RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
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


            if (!Parameters.RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
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
        public virtual bool TryGetGCp_Pos_SideWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!Parameters.WallAreaCalculator_BldgLength.effWindAreas.TryGetValue(id, out var area))
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
        public virtual bool TryGetGCp_Neg_SideWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!Parameters.WallAreaCalculator_BldgLength.effWindAreas.TryGetValue(id, out var area))
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
        public virtual bool TryGetGCp_Pos_EndWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!Parameters.WallAreaCalculator_BldgWidth.effWindAreas.TryGetValue(id, out var area))
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
        public virtual bool TryGetGCp_Neg_EndWall_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!Parameters.WallAreaCalculator_BldgWidth.effWindAreas.TryGetValue(id, out var area))
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


            if (!Parameters.RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
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


        public void CalculatePressures()
        {
            CalculateExternalPressures();
            CalculateNetPressures();
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures
        /// </summary>
        private void CalculateExternalPressures()
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

            // endwall pressure positive
            foreach (var area in Parameters.WallAreaCalculator_BldgWidth.effWindAreas)
            {
                if (TryGetGCp_Pos_EndWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureEndWall_Pos_External.Add(area.Key, pressure);
                }
            }

            // endwall pressure negative
            foreach (var area in Parameters.WallAreaCalculator_BldgWidth.effWindAreas)
            {
                if (TryGetGCp_Neg_EndWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureEndWall_Neg_External.Add(area.Key, pressure);
                }
            }

            // sidewall pressure positive
            foreach (var area in Parameters.WallAreaCalculator_BldgLength.effWindAreas)
            {
                if (TryGetGCp_Pos_SideWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureSideWall_Pos_External.Add(area.Key, pressure);
                }
            }

            // sidewall pressure negative
            foreach (var area in Parameters.WallAreaCalculator_BldgLength.effWindAreas)
            {
                if (TryGetGCp_Pos_SideWall_ByArea(area.Key, out var gcp))
                {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * gcp;
                    windPressureSideWall_Neg_External.Add(area.Key, pressure);
                }
            }
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures minus the internal pressure qh * GCpi
        /// </summary>
        private void CalculateNetPressures()
        {
            // roof pressure positive
            foreach (var items in windPressureRoof_Pos_External)
            {
                    double int_pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                windPressureRoof_Pos_Net.Add(items.Key, items.Value + int_pressure);
            }

            // roof pressure negative
            foreach (var items in windPressureRoof_Neg_External)
            {
                    double int_pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                    windPressureRoof_Neg_Net.Add(items.Key, items.Value - int_pressure);
            }

            // endwall pressure positive
            foreach (var items in windPressureEndWall_Pos_External)
            {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                    windPressureEndWall_Pos_Net.Add(items.Key, items.Value + pressure);
            }

            // endwall pressure negative
            foreach (var items in windPressureEndWall_Neg_External)
            {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                    windPressureEndWall_Neg_Net.Add(items.Key, items.Value - pressure);
            }

            // sidewall pressure positive
            foreach (var items in windPressureSideWall_Pos_External)
            {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                    windPressureSideWall_Pos_Net.Add(items.Key, items.Value - pressure);
            }

            // sidewall pressure negative
            foreach (var items in windPressureSideWall_Neg_External)
            {
                    double pressure = CalculateDynamicWindPressure(buildingData.MeanRoofHeight) * GetGCpi();
                    windPressureSideWall_Neg_Net.Add(items.Key, items.Value - pressure);
            }
        }


        /// <summary>
        /// Try to fetch a roof region by label and area
        /// </summary>
        /// <param name="label"></param>
        /// <param name="area"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private bool TryGetEffectiveWindAreaID_Roof(string label, double area, out int id)
        {
            id = -1;

            foreach(KeyValuePair<int, EffectiveWindArea> item in Parameters.RoofAreaCalculator.effWindAreas)
            {
                
                if ((item.Value.Label_Full == label) && (item.Value.Area == area))
                {
                    id = item.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to fetch a roof region by label and area
        /// </summary>
        /// <param name="label"></param>
        /// <param name="area"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private bool TryGetEffectiveWindAreaID_SideWall(string label, double area, out int id)
        {
            id = -1;

            foreach (KeyValuePair<int, EffectiveWindArea> item in Parameters.WallAreaCalculator_BldgLength.effWindAreas)
            {

                if ((item.Value.Label_Full == label) && (item.Value.Area == area))
                {
                    id = item.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to fetch a roof region by label and area
        /// </summary>
        /// <param name="label"></param>
        /// <param name="area"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private bool TryGetEffectiveWindAreaID_EndWall(string label, double area, out int id)
        {
            id = -1;

            foreach (KeyValuePair<int, EffectiveWindArea> item in Parameters.WallAreaCalculator_BldgWidth.effWindAreas)
            {

                if ((item.Value.Label_Full == label) && (item.Value.Area == area))
                {
                    id = item.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to fetch a roof region by label and area
        /// </summary>
        /// <param name="label"></param>
        /// <param name="area"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private bool TryGetEffectiveWindArea_EndWall(string label, double area, out EffectiveWindArea region)
        {
            region = null;
            foreach (KeyValuePair<int, EffectiveWindArea> item in Parameters.WallAreaCalculator_BldgWidth.effWindAreas)
            {

                if ((item.Value.Label_Full == label) && (item.Value.Area == area))
                {
                    region = item.Value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to fetch a roof region by label and area
        /// </summary>
        /// <param name="label"></param>
        /// <param name="area"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private bool TryGetEffectiveWindArea_SideWall(string label, double area, out EffectiveWindArea region)
        {
            region = null;
            foreach (KeyValuePair<int, EffectiveWindArea> item in Parameters.WallAreaCalculator_BldgLength.effWindAreas)
            {

                if ((item.Value.Label_Full == label) && (item.Value.Area == area))
                {
                    region = item.Value;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Pos_Roof(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_Roof(area.Label_Full, area.Area, out id))
            {
                if (windPressureRoof_Pos_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Neg_Roof(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_Roof(area.Label_Full, area.Area, out id))
            {
                if (windPressureRoof_Neg_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Overhang_Roof(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_Roof(area.Label_Full, area.Area, out id))
            {
                if (windPressureOverhang_External.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Pos_SideWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_SideWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureSideWall_Pos_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Neg_SideWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_SideWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureSideWall_Neg_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Pos_EndWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_EndWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureEndWall_Pos_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Neg_EndWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_EndWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureEndWall_Neg_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public string DisplayExternalPressures()
        {
            string str = "External Pressures GCp * qh:\n";

            str += "-- Positive roof pressures\n";
            foreach (var item in windPressureRoof_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative roof pressures\n";
            foreach (var item in windPressureRoof_Neg_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive end wall pressures\n";
            foreach (var item in windPressureEndWall_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative end wall pressures\n";
            foreach (var item in windPressureEndWall_Neg_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive side wall pressures\n";
            foreach (var item in windPressureSideWall_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative side wall pressures\n";
            foreach (var item in windPressureSideWall_Neg_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Overhang pressures\n";
            foreach (var item in windPressureOverhang_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            return str;
        }

        public string DisplayNetPressures()
        {
            string str = "Net Pressures GCp * qh - GCpi * qh:\n";

            str += "-- Positive roof pressures\n";
            foreach (var item in windPressureRoof_Pos_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative roof pressures\n";
            foreach (var item in windPressureRoof_Neg_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive end wall pressures\n";
            foreach (var item in windPressureEndWall_Pos_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative end wall pressures\n";
            foreach (var item in windPressureEndWall_Neg_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive side wall pressures\n";
            foreach (var item in windPressureSideWall_Pos_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative side wall pressures\n";
            foreach (var item in windPressureSideWall_Neg_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Overhang pressures\n";
            foreach (var item in windPressureOverhang_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            return str;
        }
    }
}
