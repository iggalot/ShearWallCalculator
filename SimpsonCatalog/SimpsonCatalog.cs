using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    public enum SimpsonCatalogs
    {
        SIMPSON_CATALOG_HDU = 0,  // hold down catalog for HDU_Dict family of connectoes
        SIMPSON_CATALOG_STRAP = 1, // hold down catalog for strap ties
        SIMPSON_CATALOG_HTT = 2, // hold down for HTT_Dict and LTT family of hold downs
    }
    public enum WoodTypes
    {
        WOODTYPE_DF_SP = 0,   // douglas fir / southern pine
        WOODTYPE_SPF_HF = 1   // spruce pine fir / hem fir
    }

    public class SimpsonCatalog
    {
        // Initialize the HDU_Dict dictionary
        public static HDU_Manager HDUManager = new HDU_Manager();
        public static StrapTies_Manager StrapTiesManager = new StrapTies_Manager();
        public static HTT_Manager HTTManager = new HTT_Manager();

        public List<BaseSimpsonConnectorData> GetModelsExceedingReqLoad(double req_load, SimpsonCatalogs catalogType, WoodTypes woodType)
        {
            //if (catalogType == SimpsonCatalogs.SIMPSON_CATALOG_HDU)
            //{
            //    return HDUManager.GetHDUModelsExceedingMinLoad(req_load, woodType);
            //}
            //else if (catalogType == SimpsonCatalogs.SIMPSON_CATALOG_STRAP)
            //{
            //    return StrapTiesManager.GetStrapTieModelsExceedingMinLoad(req_load, woodType);
            //}
            //else if (catalogType == SimpsonCatalogs.SIMPSON_CATALOG_HTT)
            //{
            //    return HTTManager.GetHTTModelsExceedingMinLoad(req_load, woodType);
            //}
            //else
            //{
            //    throw new ArgumentException("Invalid catalog type." + catalogType + " in SimpsonCatalog.GetModelsExceedingReqLoad()");
            //}
            return null;
        }
    }
}
