using BarberApp.Core;
using Xunit;

namespace BarberApp.Tests;

public class DeviceIdHelperTests
{
    [Fact]
    public void Misma_semilla_genera_mismo_id()
    {
        var a = DeviceIdHelper.GenerarId("machine-guid-123", "install-abc");
        var b = DeviceIdHelper.GenerarId("machine-guid-123", "install-abc");
        Assert.Equal(a, b);
        Assert.Equal(16, a.Length);
    }

    [Fact]
    public void Semilla_distinta_genera_id_distinto()
    {
        var a = DeviceIdHelper.GenerarId("machine-a", "install-abc");
        var b = DeviceIdHelper.GenerarId("machine-b", "install-abc");
        Assert.NotEqual(a, b);
    }
}
