using Newtonsoft.Json;
using System.Windows.Media;

namespace ShearWallCalculator
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class DrawableObject
    {
        [JsonProperty]
        public abstract string Type { get; }
    }
}
