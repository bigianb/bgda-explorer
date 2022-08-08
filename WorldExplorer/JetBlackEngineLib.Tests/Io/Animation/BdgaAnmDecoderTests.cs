using JetBlackEngineLib.Data.Animation;

namespace JetBlackEngineLib.Tests.Io.Animation;

[TestFixture]
internal class BdgaAnmDecoderTests
{
    BdgaAnmDecoder _sut = null!;

    [SetUp]
    public void SetupEach()
    {
        _sut = new BdgaAnmDecoder();
    }

    [Test]
    [TestCase("TestData/bdga_anim1.anm", 35)]
    public async Task Decode_Success_DecodesTestData(string testFilePath, int expectedFrameCount)
    {
        var fileData = await File.ReadAllBytesAsync(testFilePath);

        var result = _sut.Decode(fileData);

        Assert.That(result, Is.Not.Null, "A result should have been returned");
        Assert.That(result.NumFrames, Is.EqualTo(expectedFrameCount), "Animation's frame count isn't right");
    }
}