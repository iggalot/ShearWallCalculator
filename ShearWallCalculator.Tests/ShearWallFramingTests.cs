using ShearWallCalculator.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShearWallCalculator.Tests
{
    /// <summary>SDPWS-2021 4.3.7.1 item 5: a 3x face at panel joints with nails at 2" o.c., or 10d at 3" o.c. or less.</summary>
    [TestClass]
    public class ShearWallFramingTests
    {
        [TestMethod]
        public void Sdpws_WideFaceAtJoints()
        {
            Assert.IsTrue(ShearWallFraming.WideFaceAtPanelJoints(2, false));
            Assert.IsTrue(ShearWallFraming.WideFaceAtPanelJoints(3, true));
            Assert.IsFalse(ShearWallFraming.WideFaceAtPanelJoints(3, false));
            Assert.IsFalse(ShearWallFraming.WideFaceAtPanelJoints(4, true));
        }
    }
}
