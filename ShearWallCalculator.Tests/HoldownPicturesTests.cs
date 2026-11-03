using ShearWallCalculator.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShearWallCalculator.Tests
{
    /// <summary>The catalog's product pictures: one per product line, by the model's leading letters.</summary>
    [TestClass]
    public class HoldownPicturesTests
    {
        [TestMethod]
        public void LineOf_TheLeadingLetters()
        {
            Assert.AreEqual("HDUE", HoldownPictures.LineOf("HDUE5-SDS3"));
            Assert.AreEqual("HDU", HoldownPictures.LineOf("HDU2-SDS2.5"));  // the discontinued HDU: its own picture
            Assert.AreEqual("DTT", HoldownPictures.LineOf("DTT2Z-SDS2.5"));
            Assert.AreEqual("HTT", HoldownPictures.LineOf("HTT5KT"));
            Assert.AreEqual("HTTH", HoldownPictures.LineOf("HTTH6.5"));
            Assert.AreEqual("LTTP", HoldownPictures.LineOf("LTTP2"));
            Assert.AreEqual("LSTHD", HoldownPictures.LineOf("LSTHD8RJ"));
            Assert.AreEqual("ST", HoldownPictures.LineOf("ST6224"));
            Assert.AreEqual("MSTC", HoldownPictures.LineOf(" mstc66 "));
            Assert.AreEqual("", HoldownPictures.LineOf(null));
            Assert.AreEqual("", HoldownPictures.LineOf("2x4"));
        }

        [TestMethod]
        public void FileFor_LinePng_NullWithoutALine()
        {
            Assert.AreEqual("HDUE.png", HoldownPictures.FileFor("HDUE9-SDS3.5"));
            Assert.IsNull(HoldownPictures.FileFor(""));
        }
    }
}
