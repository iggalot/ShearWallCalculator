using System;
using System.Collections.Generic;

namespace ShearWallCalculator
{
    public enum SimpsonCatalogs
    {
        SIMPSON_CATALOG_HDU = 0,  // hold down catalog for HDU_Dict family of connectoes
        SIMPSON_CATALOG_STRAP = 1, // hold down catalog for strap ties
        SIMPSON_CATALOG_HTT = 2, // hold down for HTT_Dict and LTT family of hold downs
        SIMPSON_CATALOG_STHD = 3, // LSTHD / STHD strap-tie hold-downs cast into the foundation
    }
    public enum WoodTypes
    {
        WOODTYPE_DF_SP = 0,   // douglas fir / southern pine
        WOODTYPE_SPF_HF = 1   // spruce pine fir / hem fir
    }



    public class SimpsonCatalog
    {
        /// <summary>The catalog every hold-down value in the managers comes from (copy: docs/references/C-C-2026.pdf).</summary>
        public const string Edition = "C-C-2026";

        /// <summary>The full citation shown in the app, the report and the catalog listing.</summary>
        public const string Citation = "Simpson Strong-Tie, Wood Construction Connectors catalog C-C-2026 (2026–2027 edition, expires 12/31/27)";

        /// <summary>Where each manager's values are tabulated in <see cref="Edition"/>.</summary>
        public const string PagesHduDtt = "p. 61";
        public const string PagesHtt = "p. 63";
        public const string PagesSthd = "p. 67";
        public const string PagesStraps = "p. 291";

        /// <summary>
        /// The discontinued HDU-SDS2.5 hold-downs (<see cref="HDU_Legacy_Manager"/>): not in <see cref="Edition"/>; their values
        /// are from the previous edition, and Simpson's letter L-C-HDUECONV25 (copy: docs/references) discontinues them.
        /// </summary>
        public const string LegacyHduEdition = "C-C-2021";
        public const string PagesLegacyHdu = "p. 53";
        public const string LegacyHduLetter = "L-C-HDUECONV25";
        public const string LegacyHduCitation = "Simpson Strong-Tie, Wood Construction Connectors catalog C-C-2021, p. 53 (HDU discontinued: Simpson letter L-C-HDUECONV25, April 2, 2025, \"HDUE™ Holdown One-for-One Replacement for Discontinued HDU\")";

        // The mapping of our abstract managers to the approriate connector managers
        public readonly Dictionary<SimpsonCatalogs, BaseSimpsonConnectorManager> ManagerMap;

        // Initialize the HDU_Dict dictionary
        public HDU_Manager HDUManager = new HDU_Manager();
        public StrapTies_Manager StrapTiesManager = new StrapTies_Manager();
        public HTT_Manager HTTManager = new HTT_Manager();
        public STHD_Manager STHDManager = new STHD_Manager();
        public HDU_Legacy_Manager LegacyHDUManager = new HDU_Legacy_Manager();

        public SimpsonCatalog()
        {
            ManagerMap = new Dictionary<SimpsonCatalogs, BaseSimpsonConnectorManager>
            {
                { SimpsonCatalogs.SIMPSON_CATALOG_HDU, new HDU_Manager() },
                { SimpsonCatalogs.SIMPSON_CATALOG_STRAP, new StrapTies_Manager() },
                { SimpsonCatalogs.SIMPSON_CATALOG_HTT, new HTT_Manager() },
                { SimpsonCatalogs.SIMPSON_CATALOG_STHD, new STHD_Manager() }
            };
        }


    }
}
