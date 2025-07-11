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
        // The mapping of our abstract managers to the approriate connector managers
        public readonly Dictionary<SimpsonCatalogs, BaseSimpsonConnectorManager> ManagerMap;

        // Initialize the HDU_Dict dictionary
        public HDU_Manager HDUManager = new HDU_Manager();
        public StrapTies_Manager StrapTiesManager = new StrapTies_Manager();
        public HTT_Manager HTTManager = new HTT_Manager();

        public SimpsonCatalog()
        {
            ManagerMap = new Dictionary<SimpsonCatalogs, BaseSimpsonConnectorManager>
            {
                { SimpsonCatalogs.SIMPSON_CATALOG_HDU, new HDU_Manager() },
                { SimpsonCatalogs.SIMPSON_CATALOG_STRAP, new StrapTies_Manager() },
                { SimpsonCatalogs.SIMPSON_CATALOG_HTT, new HTT_Manager() }
            };
        }


    }
}
