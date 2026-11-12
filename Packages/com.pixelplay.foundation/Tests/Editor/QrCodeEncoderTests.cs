using System;
using NUnit.Framework;
using PixelPlay.Foundation.QrCodes;

namespace PixelPlay.Foundation.Tests
{
    public sealed class QrCodeEncoderTests
    {
        [Test]
        public void EncodeText_ReturnsDeterministicSquareMatrixWithFinderPatterns()
        {
            var encoder = new QrCodeEncoder();
            QrCodeMatrix first = encoder.EncodeText("hello");
            QrCodeMatrix second = encoder.EncodeText("hello");

            Assert.That(first.Version, Is.EqualTo(1));
            Assert.That(first.ModuleCount, Is.EqualTo(21));
            Assert.That(first.IsDark(0, 0), Is.True);
            Assert.That(first.IsDark(3, 3), Is.True);
            Assert.That(first.IsDark(1, 1), Is.False);
            for (int y = 0; y < first.ModuleCount; y++)
                for (int x = 0; x < first.ModuleCount; x++)
                    Assert.That(first.IsDark(x, y), Is.EqualTo(second.IsDark(x, y)));
        }

        [Test]
        public void EncodeText_RejectsEmptyAndOverCapacityPayloads()
        {
            var encoder = new QrCodeEncoder();
            Assert.Throws<ArgumentException>(() => encoder.EncodeText(string.Empty));
            Assert.Throws<ArgumentException>(() => encoder.EncodeText(new string('x', 107)));
        }
    }
}
