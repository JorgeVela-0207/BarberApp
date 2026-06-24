using BarberApp.Core;

using Xunit;

namespace BarberApp.Tests;

public class CitaHorarioHelperTests
{
    [Fact]
    public void HaySolapamiento_detecta_conflicto()
    {
        var a = TimeSpan.FromHours(10);
        var b = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(15));
        Assert.True(CitaHorarioHelper.HaySolapamiento(a, 30, b, 30));
    }

    [Fact]
    public void ParseHora_acepta_formato_hh_mm()
    {
        Assert.True(CitaHorarioHelper.ParseHora("14:30", out var t));
        Assert.Equal(new TimeSpan(14, 30, 0), t);
    }
}
