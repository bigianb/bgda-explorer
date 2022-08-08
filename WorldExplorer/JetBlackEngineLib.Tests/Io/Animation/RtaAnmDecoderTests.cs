using JetBlackEngineLib.Data.Animation;

namespace JetBlackEngineLib.Tests.Io.Animation;

[TestFixture]
internal class RtaAnmDecoderTests
{
    RtaAnmDecoder _sut = null!;

    [SetUp]
    public void SetupEach()
    {
        _sut = new RtaAnmDecoder();
    }

    [Test]
    [TestCase("TestData/rta_anim1.anm", 150)]
    public async Task Decode_Success_DecodesTestData(string testFilePath, int expectedFrameCount)
    {
        var fileData = await File.ReadAllBytesAsync(testFilePath);

        var result = _sut.Decode(fileData);

        Assert.That(result, Is.Not.Null, "A result should have been returned");
        Assert.That(result.NumFrames, Is.EqualTo(expectedFrameCount), "Animation's frame count isn't right");
    }
}