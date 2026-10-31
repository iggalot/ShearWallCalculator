using ASCE7WindLoadCalculator;
using calculator;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace ShearWallCalculator.Interfaces
{
    /// <summary>
    /// One project file: the wind inputs, the shear-wall drawing (walls, diaphragms, load cases) and where the
    /// loads came from. Format 2. Files without "FormatVersion" are the older drawing-only files: they still open,
    /// with no wind inputs.
    /// </summary>
    public class ProjectFile
    {
        public const int CurrentFormat = 2;

        public int FormatVersion { get; set; } = CurrentFormat;

        public string Application { get; set; } = "ShearWallCalculator";

        public WindInputs Wind { get; set; }

        /// <summary>The shear-wall calculator, in the same JSON the older drawing files hold.</summary>
        public JObject Walls { get; set; }

        public LoadsInfo Loads { get; set; }
    }

    public class WindInputs
    {
        public ASCE7_Versions Edition { get; set; } = ASCE7_Versions.ASCE_VER_7_22;
        public WindSiteInputs Site { get; set; } = new WindSiteInputs();
        public WindBuildingInputs Building { get; set; } = new WindBuildingInputs();
        public bool OnlyCases1And3 { get; set; }
        /// <summary>MWFRS procedure for the shear-wall loads; Chapter 27 when absent in older files.</summary>
        public LateralProcedure LateralProcedure { get; set; } = LateralProcedure.Directional;
        public ComponentChoice RoofComponent { get; set; }
        public ComponentChoice WallComponent { get; set; }

        public static WindInputs From(ASCE7_Versions edition, WindParameters_Base p, BuildingData b, bool onlyCases1And3,
            ComponentChoice roof, ComponentChoice wall) => new WindInputs
        {
            Edition = edition,
            Site = new WindSiteInputs
            {
                RiskCategory = p.RiskCategory,
                WindSpeed = p.WindSpeed,
                Exposure = p.ExposureCategory,
                Kzt = p.Kzt,
                GustFactor = p.GustFactor,
                RoofComponentArea = p.RoofComponentArea,
                WallComponentArea = p.WallComponentArea,
            },
            Building = new WindBuildingInputs
            {
                Length = b.BuildingLength,
                Width = b.BuildingWidth,
                EaveHeight = b.BuildingHeight,
                OverhangWidth = b.OverhangWidth,
                ParapetHeight = b.ParapetHeight,
                ParapetEnclosure = b.ParapetEnclosure,
                RoofPitch = b.RoofPitch,
                RoofType = b.RoofType,
                Ridge = b.RidgeDirection,
                Enclosure = b.EnclosureType,
            },
            OnlyCases1And3 = onlyCases1And3,
            LateralProcedure = p.LateralProcedure,
            RoofComponent = roof,
            WallComponent = wall,
        };

        public WindParameters_Base ToParameters() => new WindParameters_Base
        {
            RiskCategory = Site.RiskCategory,
            WindSpeed = Site.WindSpeed,
            ExposureCategory = Site.Exposure,
            Kzt = Site.Kzt,
            GustFactor = Site.GustFactor,
            RoofComponentArea = Site.RoofComponentArea,
            WallComponentArea = Site.WallComponentArea,
            LateralProcedure = LateralProcedure,
        };

        public BuildingData ToBuilding() => new BuildingData
        {
            BuildingLength = Building.Length,
            BuildingWidth = Building.Width,
            BuildingHeight = Building.EaveHeight,
            OverhangWidth = Building.OverhangWidth,
            ParapetHeight = Building.ParapetHeight,
            ParapetEnclosure = Building.ParapetEnclosure,
            RoofPitch = Building.RoofPitch,
            RoofType = Building.RoofType,
            RidgeDirection = Building.Ridge,
            EnclosureType = Building.Enclosure,
        };
    }

    public class WindSiteInputs
    {
        public string RiskCategory { get; set; } = "II";
        public double WindSpeed { get; set; } = 115;
        public WindExposureCategories Exposure { get; set; } = WindExposureCategories.WIND_EXP_CAT_C;
        public double Kzt { get; set; } = 1.0;
        public double GustFactor { get; set; } = 0.85;
        public double RoofComponentArea { get; set; } = WindParameters_Base.DefaultComponentArea;
        public double WallComponentArea { get; set; } = WindParameters_Base.DefaultComponentArea;
    }

    public class WindBuildingInputs
    {
        public double Length { get; set; } = 60;
        public double Width { get; set; } = 40;
        public double EaveHeight { get; set; } = 15;
        /// <summary>Roof overhang beyond the wall face (ft); 0 for none (absent in older files).</summary>
        public double OverhangWidth { get; set; }
        /// <summary>Parapet height above a flat roof (ft); 0 for none (absent in older files).</summary>
        public double ParapetHeight { get; set; }
        /// <summary>Parapet envelope porosity for its C&amp;C internal pressure (30.6); enclosed when absent in older files.</summary>
        public BuildingEnclosures ParapetEnclosure { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;
        public double RoofPitch { get; set; } = 15;
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_FLAT;
        public RidgeDirections Ridge { get; set; } = RidgeDirections.RIDGE_DIR_NONE;
        public BuildingEnclosures Enclosure { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;
    }

    public class LoadsInfo
    {
        /// <summary>True when the walls took their loads from the wind calculation (they are re-linked on open).</summary>
        public bool LinkedToWind { get; set; }

        /// <summary>The loads when saved (kips), to tell the user if the wind calculation now gives different ones.</summary>
        public double Vx { get; set; }
        public double Vy { get; set; }
    }

    /// <summary>What <see cref="ProjectSerializer.Read"/> found in a file.</summary>
    public class ProjectContents
    {
        public ShearWallCalculatorBase Calculator { get; set; }

        /// <summary>Null for an older drawing-only file.</summary>
        public WindInputs Wind { get; set; }

        public LoadsInfo Loads { get; set; }

        public bool IsOlderDrawingFile => Wind == null;
    }

    public static class ProjectSerializer
    {
        private static JsonSerializerSettings Settings() => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented,
            Converters = { new DrawableObjectConverter(), new StringEnumConverter() },
        };

        /// <summary>The file text for this project (also used to tell whether there are unsaved changes).</summary>
        public static string ToJson(ShearWallCalculatorBase calculator, WindInputs wind, bool linkedToWind)
        {
            var settings = Settings();
            var file = new ProjectFile
            {
                Wind = wind,
                Walls = calculator == null ? null : JObject.Parse(JsonConvert.SerializeObject(calculator, settings)),
                Loads = new LoadsInfo { LinkedToWind = linkedToWind, Vx = calculator?.V_x ?? 0, Vy = calculator?.V_y ?? 0 },
            };
            return JsonConvert.SerializeObject(file, settings);
        }

        public static void Save(string path, ShearWallCalculatorBase calculator, WindInputs wind, bool linkedToWind) =>
            File.WriteAllText(path, ToJson(calculator, wind, linkedToWind));

        public static ProjectContents Load(string path) => Read(File.ReadAllText(path));

        /// <summary>Reads a project file, or an older drawing-only file.</summary>
        /// <exception cref="InvalidDataException">The text is not a project or drawing file.</exception>
        public static ProjectContents Read(string json)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("This is not a shear wall project file (it is not valid JSON).", ex);
            }

            var settings = Settings();
            if (root["FormatVersion"] == null)
                return new ProjectContents { Calculator = ReadCalculator(root, settings) };

            int format = (int)root["FormatVersion"];
            if (format > CurrentFormatSupported)
                throw new InvalidDataException($"This file was saved by a newer version of the program (format {format}).");

            var file = root.ToObject<ProjectFile>(JsonSerializer.Create(settings));
            return new ProjectContents
            {
                Calculator = file.Walls == null ? new ShearWallCalculator_RigidDiaphragm() : ReadCalculator(file.Walls, settings),
                Wind = file.Wind,
                Loads = file.Loads,
            };
        }

        private const int CurrentFormatSupported = ProjectFile.CurrentFormat;

        private static ShearWallCalculatorBase ReadCalculator(JObject obj, JsonSerializerSettings settings)
        {
            string json = obj.ToString();
            switch ((string)obj["CalculatorType"])
            {
                case "Rigid Diaphragm":
                    return new ShearWallCalculator_RigidDiaphragm(JsonConvert.DeserializeObject<ShearWallCalculator_RigidDiaphragm>(json, settings));
                case "Flexible Diaphragm":
                    return new ShearWallCalculator_FlexibleDiaphragm(JsonConvert.DeserializeObject<ShearWallCalculator_FlexibleDiaphragm>(json, settings));
                default:
                    throw new InvalidDataException("The file has no shear wall drawing (unknown calculator type).");
            }
        }
    }
}
