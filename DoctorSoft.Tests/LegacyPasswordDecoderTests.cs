using DoctorSoft.Data.Security;

namespace DoctorSoft.Tests;

public class LegacyPasswordDecoderTests
{
    [Fact]
    public void Decode_ReturnsEmpty_ForNullOrWhitespace()
    {
        var decoder = new LegacyPasswordDecoder();

        Assert.Equal(string.Empty, decoder.Decode(""));
        Assert.Equal(string.Empty, decoder.Decode("   "));
    }

    [Fact]
    public void Decode_ReturnsEmpty_WhenDelimitersMissing()
    {
        var decoder = new LegacyPasswordDecoder();

        var result = decoder.Decode("abc123");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decode_IgnoresInvalidTokens_AndDecodesValidTokens()
    {
        var decoder = new LegacyPasswordDecoder();
        var encoded = "$9.857142857142858$$invalid$$15$";

        var result = decoder.Decode(encoded);

        Assert.Equal("Ei", result);
    }

    [Fact]
    public void Decode_StopsWhenClosingDelimiterMissing()
    {
        var decoder = new LegacyPasswordDecoder();

        var result = decoder.Decode("$9.857142857142858$incomplete$");

        Assert.Equal("E", result);
    }
}
