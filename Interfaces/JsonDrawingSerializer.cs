using calculator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ShearWallCalculator.Interfaces
{
    public class JsonDrawingSerializer
    {
        private JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented,
            Converters = { new DrawableObjectConverter() }
        };

        public void Save(string filePath, ShearWallCalculatorBase data)
        {
            var json = JsonConvert.SerializeObject(data, _settings);
            File.WriteAllText(filePath, json);
        }

        public string Load(string filePath)
        {
            return File.ReadAllText(filePath);
        }
    }

}
