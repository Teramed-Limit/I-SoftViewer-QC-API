using ISoftViewerLibrary.Model.DicomOperator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class TextMaskRendererTest
    {
        [TestMethod]
        public void RenderMask_L_ReturnsNonEmptyMask()
        {
            var (mask, width, height) = TextMaskRenderer.RenderMask("L", 24);

            Assert.IsNotNull(mask);
            Assert.IsTrue(width > 0);
            Assert.IsTrue(height > 0);
            Assert.AreEqual(width * height, mask.Length);

            bool hasWhitePixels = false;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] > 128) { hasWhitePixels = true; break; }
            }
            Assert.IsTrue(hasWhitePixels, "Mask should contain white pixels for the text");
        }

        [TestMethod]
        public void RenderMask_R_ReturnsNonEmptyMask()
        {
            var (mask, width, height) = TextMaskRenderer.RenderMask("R", 48);

            Assert.IsNotNull(mask);
            Assert.IsTrue(width > 0);
            Assert.IsTrue(height > 0);

            bool hasWhitePixels = false;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] > 128) { hasWhitePixels = true; break; }
            }
            Assert.IsTrue(hasWhitePixels);
        }

        [TestMethod]
        public void RenderMask_LargerFont_ProducesLargerMask()
        {
            var (_, w1, h1) = TextMaskRenderer.RenderMask("L", 12);
            var (_, w2, h2) = TextMaskRenderer.RenderMask("L", 48);

            Assert.IsTrue(w2 > w1 || h2 > h1,
                "Larger font should produce larger mask dimensions");
        }
    }
}
