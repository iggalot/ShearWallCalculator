using calculator;
using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    public enum ConnectorTypes
    {
        CONNECTOR_HDU = 0,  // HDU_Dict foundation connectors for walls to foundation
        CONNECTOR_HTT = 1,  // HTT_Dict tension connections for upper walls to lower walls
        CONNECTOR_STRAP_TIES = 2, // connections for strapping
    }
    public class ShearWallSelector
    {
        public SimpsonCatalog Catalog { get; set; }
        public ConnectorTypes connectorType { get; set; }
        private double Height => Data.WallHeight;
        private double Length => Data.WallLength;
        private double Vu { get; set; } = 0;
        public double? reqHoldDownForce { get => ComputeHolddownForces(); }
        public double? reqHorizShearForce { get => ComputeHorizontalShear(); }
        public WallData Data { get; set; }

        public List<BaseSimpsonConnectorData> selectedConnectors = new List<BaseSimpsonConnectorData>();

        /// <summary> 
        /// 
        /// </summary>
        /// <param name="horiz_shear_force">the computed design horizontal shear force for the wall section</param>
        /// <param name="wall_data">The wall data of the selected wall <see cref="WallData"/></param>
        /// <param name="catalog">The current Simposon catalog</param>
        /// <param name="connector_type">the class of connect <see cref="ConnectorTypes"/></param>
        /// <param name="woodType">the type of wood <see cref="WoodTypes"/></param>
        public ShearWallSelector(double horiz_shear_force, WallData wall_data, SimpsonCatalog catalog, ConnectorTypes connector_type, WoodTypes woodType = WoodTypes.WOODTYPE_DF_SP)
        {
            this.Data = wall_data;
            this.Vu = horiz_shear_force;
            this.connectorType = connector_type;
            this.Catalog = catalog;

            switch (connector_type)
            {
                case ConnectorTypes.CONNECTOR_HDU:
                    this.selectedConnectors = GetValidConnectors(SimpsonCatalogs.SIMPSON_CATALOG_HDU, horiz_shear_force, woodType);
                    break;
                case ConnectorTypes.CONNECTOR_HTT:
                    this.selectedConnectors = GetValidConnectors(SimpsonCatalogs.SIMPSON_CATALOG_HTT, horiz_shear_force, woodType);
                    break;
                case ConnectorTypes.CONNECTOR_STRAP_TIES:
                    this.selectedConnectors = GetValidConnectors(SimpsonCatalogs.SIMPSON_CATALOG_STRAP, horiz_shear_force, woodType);
                    break;
            }
        }

        public double ComputeHolddownForces()
        {
            return Vu * Height / Length;
        }

        /// <summary>
        /// Computes the horizontal shear requirement for connector(s) on one end of the wall
        /// </summary>
        /// <returns></returns>
        public double ComputeHorizontalShear()
        {
            return Vu / 2;
        }

        public List<BaseSimpsonConnectorData> GetValidConnectors(
            SimpsonCatalogs catalogType,
            double reqLoad,
            WoodTypes woodType)
        {
            if (!Catalog.ManagerMap.TryGetValue(catalogType, out BaseSimpsonConnectorManager manager))
            {
                throw new ArgumentException($"Invalid catalog type: {catalogType}");
            }

            return manager.GetModelsExceedingMinLoad(reqLoad, woodType);
        }

        //public List<BaseSimpsonConnectorData> GetModelsExceedingReqLoad(double req_load, SimpsonCatalogs catalogType, WoodTypes woodType)
        //{
        //    //if (catalogType == SimpsonCatalogs.SIMPSON_CATALOG_HDU)
        //    //{
        //    //    return HDUManager.GetHDUModelsExceedingMinLoad(req_load, woodType);
        //    //}
        //    //else if (catalogType == SimpsonCatalogs.SIMPSON_CATALOG_STRAP)
        //    //{
        //    //    return StrapTiesManager.GetStrapTieModelsExceedingMinLoad(req_load, woodType);
        //    //}
        //    //else if (catalogType == SimpsonCatalogs.SIMPSON_CATALOG_HTT)
        //    //{
        //    //    return HTTManager.GetHTTModelsExceedingMinLoad(req_load, woodType);
        //    //}
        //    //else
        //    //{
        //    //    throw new ArgumentException("Invalid catalog type." + catalogType + " in SimpsonCatalog.GetModelsExceedingReqLoad()");
        //    //}
        //    return null;
        //}
    }
}
