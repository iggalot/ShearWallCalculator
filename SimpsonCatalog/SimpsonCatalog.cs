using System;

namespace ShearWallCalculator
{
    public enum WoodTypes
    {
        WOODTYPE_DF_SP = 0,   // douglas fir / southern pine
        WOODTYPE_SPF_HF = 1   // spruce pine fir / hem fir
    }

    public class SimpsonCatalog
    {
        // Initialize the HDU dictionary
        public static HDU_Manager HDUManager = new HDU_Manager();
        public static StrapTies_Manager StrapTiesManager = new StrapTies_Manager();

        public SimpsonCatalog()
        {
            foreach (string model in HDUManager.GetHDUModelsExceedingMinLoad(4000, WoodTypes.WOODTYPE_DF_SP))
            {
                Console.WriteLine(model);
            }

            Console.WriteLine("-----------------------------------");

            foreach (string model in StrapTiesManager.GetStrapTieModelsExceedingMinLoad(4000, WoodTypes.WOODTYPE_DF_SP))
            {
                Console.WriteLine(model);
            }
        }
    }
}
