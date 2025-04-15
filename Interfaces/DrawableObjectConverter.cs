using calculator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace ShearWallCalculator.Interfaces
{
    public class DrawableObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DrawableObject);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //JObject jo = JObject.Load(reader);
            //string type = (string)jo["Type"];

            DrawableObject result = new WallData();
            //switch (type)
            //{
            //    case "Line":
            //        //result = new WallData();
            //        break;
            //    case "Rectangle":
            //        //result = new DiaphragmData_Rectangular();
            //        break;
            //    default:
            //        throw new NotSupportedException("Unknown type: " + type);
            //}

            //serializer.Populate(jo.CreateReader(), result);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject jo = JObject.FromObject(value, serializer);
            jo.WriteTo(writer);
        }
    }
}
