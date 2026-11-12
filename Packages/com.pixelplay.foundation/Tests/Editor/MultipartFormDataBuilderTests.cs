using System;
using System.Text;
using NUnit.Framework;
using PixelPlay.Foundation.Networking;

namespace PixelPlay.Foundation.Tests
{
    public sealed class MultipartFormDataBuilderTests
    {
        [Test]
        public void BuildRequest_ContainsTextAndFilePartsWithGeneratedBoundary()
        {
            var builder = new MultipartFormDataBuilder()
                .AddField("name", "Pixel Play")
                .AddFile("image", "photo.png", "image/png", new byte[] { 1, 2, 3 });

            HttpRequestData request = builder.BuildRequest("https://example.com/upload");
            string body = Encoding.UTF8.GetString(request.Body);
            string contentType = request.Headers["Content-Type"];
            string boundary = contentType.Substring(contentType.IndexOf("boundary=", StringComparison.Ordinal) + "boundary=".Length);

            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(contentType, Does.StartWith("multipart/form-data; boundary="));
            Assert.That(body, Does.Contain("name=\"name\""));
            Assert.That(body, Does.Contain("Pixel Play"));
            Assert.That(body, Does.Contain("filename=\"photo.png\""));
            Assert.That(body, Does.Contain("Content-Type: image/png"));
            Assert.That(body, Does.Contain("--" + boundary + "--"));
        }

        [Test]
        public void BuildRequest_RequiresAtLeastOnePart()
        {
            Assert.Throws<InvalidOperationException>(() => new MultipartFormDataBuilder().BuildRequest("https://example.com"));
        }
    }
}
