using Newtonsoft.Json;
using System.Windows.Media;

namespace ShearWallCalculator
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class DrawableObject
    {
        public abstract int ID {get; set;}
        [JsonProperty]
        public abstract string Type { get; }
    }
}
