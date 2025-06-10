using System.Collections.Generic;
using System.Linq;

namespace ShearWallCalculator
{
    public abstract class BaseSimpsonConnectorManager
    {
        public abstract List<BaseSimpsonConnectorData> GetModelsExceedingMinLoad(double reqLoad, WoodTypes woodType);

        public abstract List<Dictionary<string, BaseSimpsonConnectorData>> GetValidConnectors();
    }

    public abstract class BaseSimpsonConnectorManager<T> : BaseSimpsonConnectorManager
    where T : BaseSimpsonConnectorData
    {
        public abstract List<T> GetTypedModelsExceedingMinLoad(double reqLoad, WoodTypes woodType);

        public override List<BaseSimpsonConnectorData> GetModelsExceedingMinLoad(double reqLoad, WoodTypes woodType)
        {
            return GetTypedModelsExceedingMinLoad(reqLoad, woodType).Cast<BaseSimpsonConnectorData>().ToList();
        }
    }
}
