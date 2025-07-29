using ShearWallCalculator.BuildingInfo;
using ShearWallCalculator.WindLoadCalculations.Chapter30.AreaCalculator;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ShearWallCalculator.WindLoadCalculations
{
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
        public abstract ASCE7_Versions ASCEVersion { get; }
        public abstract WindParameters_Base Parameters { get; set; }
        public abstract BuildingData buildingData { get; set; }

        /// <summary>
        /// Contains the calculator that will be used to calculate the effective wind areas on the roof
        /// </summary>
        public abstract AreaCalculator_Base RoofAreaCalculator { get; set; }

        /// <summary>
        /// Contains the calculator for the wall loads acting on the BuildingLength dimension
        /// </summary>
        public abstract AreaCalculator_Base WallAreaCalculator_BldgLength { get; set; }
        /// <summary>
        /// Contains the calculator for the wall loads acting on the BuildingWidth dimension
        /// </summary>
        public abstract AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }





        public Dictionary<int, double> windPressureRoof_Pos_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureRoof_Neg_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingLengthWall_Pos_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingLengthWall_Neg_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingWidthWall_Pos_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingWidthWall_Neg_External { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureOverhang_External { get; set; } = new Dictionary<int, double>();

        public Dictionary<int, double>  windPressureRoof_Pos_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureRoof_Neg_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingLength_Pos_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingLengthWall_Neg_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingWidthWall_Pos_Net { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double> windPressureBuildingWidthWall_Neg_Net { get; set; } = new Dictionary<int, double>();

        /// <summary>
        /// Calculates the dyanmic wind pressure q at a specified height z per ASCE7_16 and ASCE7_22
        /// </summary>
        /// <param name="p"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public abstract double CalculateDynamicWindPressure(double z);
        // Get Kz approximation based on building height and exposure category
        public abstract double GetKz(double z, WindExposureCategories exposure);

        /// <summary>
        /// Compute Kd coefficient for the specific version of ASCE7
        /// </summary>
        /// <param name="calc_type"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public abstract double GetKd(WindLoadCalculationTypes calc_type);

        /// <summary>
        /// The +/- coefficient for internal pressure coefficient GCpi from ASCE7_16 & ASCE7-22 Table 26.13-1
        /// </summary>
        /// <returns></returns>
        public abstract double GetGCpi();


        public void CreateAreaCalculators()
        {
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(buildingData, Parameters, ASCEVersion);
            RoofAreaCalculator.ComputeEffectiveWindAreas();

            bool length_is_gable = false;
            bool width_is_gable = false;
            if(buildingData.BuildingLength > buildingData.BuildingWidth)
            {
                width_is_gable = true;
            } else if (buildingData.BuildingLength < buildingData.BuildingWidth)
            {
                length_is_gable = true;
            } else
            {
                // if it's a square building we arbitrarily assign the gable to the building width
                width_is_gable = true;
            }

            Dictionary<string, double> bldg_length_wall_param = new Dictionary<string, double>();
            bldg_length_wall_param.Add("WallLength", buildingData.BuildingLength);
            WallAreaCalculator_BldgLength = WallAreaCalculatorFactory.Create(buildingData, Parameters, ASCEVersion, length_is_gable);
            WallAreaCalculator_BldgLength.ComputeEffectiveWindAreas();

            Dictionary<string, double> bldg_width_wall_param = new Dictionary<string, double>();
            bldg_width_wall_param.Add("WallLength", buildingData.BuildingWidth);
            WallAreaCalculator_BldgWidth = WallAreaCalculatorFactory.Create(buildingData, Parameters, ASCEVersion, width_is_gable);
            WallAreaCalculator_BldgWidth.ComputeEffectiveWindAreas();
        }




        public void CalculatePressures()
        {
            CalculateExternalPressures();
            CalculateNetPressures();
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures
        /// </summary>
        public abstract void CalculateExternalPressures();

        /// <summary>
        /// Calculates the qh * GCP for external pressures minus the internal pressure qh * GCpi
        /// </summary>
        public abstract void CalculateNetPressures();










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

            foreach(KeyValuePair<int, EffectiveWindArea> item in RoofAreaCalculator.effWindAreas)
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
        private bool TryGetEffectiveWindAreaID_BuildingLengthWall(string label, double area, out int id)
        {
            id = -1;

            foreach (KeyValuePair<int, EffectiveWindArea> item in WallAreaCalculator_BldgLength.effWindAreas)
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
        private bool TryGetEffectiveWindAreaID_BuildingWidthWall(string label, double area, out int id)
        {
            id = -1;

            foreach (KeyValuePair<int, EffectiveWindArea> item in WallAreaCalculator_BldgWidth.effWindAreas)
            {

                if ((item.Value.Label_Full == label) && (item.Value.Area == area))
                {
                    id = item.Key;
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

        public bool TryGetPressureNet_MostPositiveByAreaName_Roof(string area_name, out double pressure)
        {
            int id;
            pressure = double.MinValue;
            
            AreaCalculator_Base calc = RoofAreaCalculator;

            // get all ids that match the label
            bool found = false;
            foreach (var item in calc.effWindAreas)
            {
                if (item.Value.Label_Full == area_name)
                {
                    if(TryGetEffectiveWindAreaID_Roof(item.Value.Label_Full, item.Value.Area, out id))
                    {
                        double new_pressure;
                        found = windPressureRoof_Pos_Net.TryGetValue(id, out new_pressure);
                        if (found)
                        {
                            if(new_pressure > pressure)
                            {
                                pressure = new_pressure;
                            }
                        }

                    }
                }
            }

            return found;
        }

        public bool TryGetPressureNet_MostPositiveByAreaName_BuildingLengthWall(string area_name, out double pressure)
        {
            int id;
            pressure = double.MinValue;

            AreaCalculator_Base calc = WallAreaCalculator_BldgLength;

            // get all ids that match the label
            bool found = false;
            int found_count = 0;
            foreach (var item in calc.effWindAreas)
            {
                if (item.Value.Label_Full == area_name)
                {
                    if (TryGetEffectiveWindAreaID_BuildingLengthWall(item.Value.Label_Full, item.Value.Area, out id))
                    {
                        double new_pressure;
                        found = windPressureBuildingLength_Pos_Net.TryGetValue(id, out new_pressure);
                        if (found)
                        {
                            found_count++;
                            if (new_pressure > pressure)
                            {
                                pressure = new_pressure;
                            }
                        }
                    }
                }
            }
            //Console.WriteLine(found_count + " matches found for " + area_name + "!");
            return found;
        }
        public bool TryGetPressureNet_MostPositiveByAreaName_BuildingWidthWall(string area_name, out double pressure)
        {
            int id;
            pressure = double.MinValue;

            AreaCalculator_Base calc = WallAreaCalculator_BldgWidth;

            // get all ids that match the label
            bool found = false;
            int found_count = 0;
            foreach (var item in calc.effWindAreas)
            {
                if (item.Value.Label_Full == area_name)
                {
                    if (TryGetEffectiveWindAreaID_BuildingWidthWall(item.Value.Label_Full, item.Value.Area, out id))
                    {
                        double new_pressure;
                        found = windPressureBuildingWidthWall_Pos_Net.TryGetValue(id, out new_pressure);
                        if (found)
                        {
                            found_count++;
                            if (new_pressure > pressure)
                            {
                                pressure = new_pressure;
                            }
                        }
                    }
                }
            }
            //Console.WriteLine(found_count + " matches found for " + area_name + "!");
            return found;
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

        public bool TryGetPressureNet_MostNegativeByAreaName_Roof(string area_name, out double pressure)
        {
            int id;
            pressure = double.MaxValue;

            AreaCalculator_Base calc = RoofAreaCalculator;

            // get all ids that match the label
            bool found = false;
            int found_count = 0;
            foreach (var item in calc.effWindAreas)
            {
                if (item.Value.Label_Full == area_name)
                {
                    if (TryGetEffectiveWindAreaID_Roof(item.Value.Label_Full, item.Value.Area, out id))
                    {
                        double new_pressure;
                        found = windPressureRoof_Neg_Net.TryGetValue(id, out new_pressure);
                        if (found)
                        {
                            found_count++;
                            if (new_pressure < pressure)
                            {
                                pressure = new_pressure;
                            }
                        }
                    }
                }
            }
            //Console.WriteLine(found_count + " matches found for " + area_name + "!");
            return found;
        }

        public bool TryGetPressureNet_MostNegativeByAreaName_BuildingLengthWall(string area_name, out double pressure)
        {
            int id;
            pressure = double.MaxValue;

            AreaCalculator_Base calc = WallAreaCalculator_BldgLength;

            // get all ids that match the label
            bool found = false;
            int found_count = 0;
            foreach (var item in calc.effWindAreas)
            {
                if (item.Value.Label_Full == area_name)
                {
                    if (TryGetEffectiveWindAreaID_BuildingLengthWall(item.Value.Label_Full, item.Value.Area, out id))
                    {
                        double new_pressure;
                        found = windPressureBuildingLengthWall_Neg_Net.TryGetValue(id, out new_pressure);
                        if (found)
                        {
                            found_count++;
                            if (new_pressure < pressure)
                            {
                                pressure = new_pressure;
                            }
                        }
                    }
                }
            }
            //Console.WriteLine(found_count + " matches found for " + area_name + "!");
            return found;
        }

        public bool TryGetPressureNet_MostNegativeByAreaName_BuildingWidthWall(string area_name, out double pressure)
        {
            int id;
            pressure = double.MaxValue;

            AreaCalculator_Base calc = WallAreaCalculator_BldgWidth;

            // get all ids that match the label
            bool found = false;
            int found_count = 0;
            foreach (var item in calc.effWindAreas)
            {
                if (item.Value.Label_Full == area_name)
                {
                    if (TryGetEffectiveWindAreaID_BuildingWidthWall(item.Value.Label_Full, item.Value.Area, out id))
                    {
                        double new_pressure;
                        found = windPressureBuildingWidthWall_Neg_Net.TryGetValue(id, out new_pressure);
                        if (found)
                        {
                            found_count++;
                            if (new_pressure < pressure)
                            {
                                pressure = new_pressure;
                            }
                        }
                    }
                }
            }
            //Console.WriteLine(found_count + " matches found for " + area_name + "!");
            return found;
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

        public bool TryGetPressureNet_Pos_BuildingLengthWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_BuildingLengthWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureBuildingLength_Pos_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Neg_BuildingLengthWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_BuildingLengthWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureBuildingLengthWall_Neg_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Pos_BuildingWidthWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_BuildingWidthWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureBuildingWidthWall_Pos_Net.TryGetValue(id, out pressure))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPressureNet_Neg_BuildingWidthWall(EffectiveWindArea area, out double pressure)
        {
            int id;
            pressure = 0;

            if (TryGetEffectiveWindAreaID_BuildingWidthWall(area.Label_Full, area.Area, out id))
            {
                if (windPressureBuildingWidthWall_Neg_Net.TryGetValue(id, out pressure))
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
            foreach (var item in windPressureBuildingWidthWall_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative end wall pressures\n";
            foreach (var item in windPressureBuildingWidthWall_Neg_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive side wall pressures\n";
            foreach (var item in windPressureBuildingLengthWall_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative side wall pressures\n";
            foreach (var item in windPressureBuildingLengthWall_Neg_External)
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

            str += "-- Positive BuildingWidth pressures\n";
            foreach (var item in windPressureBuildingWidthWall_Pos_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative BuildingWidth pressures\n";
            foreach (var item in windPressureBuildingWidthWall_Neg_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive BuildingLength pressures\n";
            foreach (var item in windPressureBuildingLength_Pos_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative BuildingLength pressures\n";
            foreach (var item in windPressureBuildingLengthWall_Neg_Net)
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
