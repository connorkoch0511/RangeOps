using RangeOps.Core.Telemetry;
using Xunit;

namespace RangeOps.Tests;

public class TelemetryReadingTests
{
    [Fact]
    public void Parse_ValidLine_DecodesAllFields()
    {
        var line = "{\"alt_ft\":12345.6,\"airspeed_kt\":320.4,\"vs_fpm\":1800.0,\"link_dropout\":false}";

        var reading = TelemetryReading.Parse(line);

        Assert.NotNull(reading);
        Assert.Equal(12345.6, reading!.Value.AltitudeFt, 1);
        Assert.Equal(320.4, reading.Value.AirspeedKt, 1);
        Assert.Equal(1800.0, reading.Value.VerticalSpeedFpm, 1);
        Assert.False(reading.Value.LinkDropout);
    }

    [Fact]
    public void Parse_LinkDropoutTrue_SetsFlag()
    {
        var reading = TelemetryReading.Parse(
            "{\"alt_ft\":674.8,\"airspeed_kt\":184.4,\"vs_fpm\":2036.9,\"link_dropout\":true}");

        Assert.NotNull(reading);
        Assert.True(reading!.Value.LinkDropout);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("{\"alt_ft\":1.0}")]          // missing required fields
    [InlineData("{\"alt_ft\":\"oops\",\"airspeed_kt\":1,\"vs_fpm\":1,\"link_dropout\":false}")]
    public void Parse_MalformedOrPartial_ReturnsNull(string? line)
    {
        Assert.Null(TelemetryReading.Parse(line));
    }
}
