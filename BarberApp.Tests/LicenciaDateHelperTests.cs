using BarberApp.Core;
using Xunit;

namespace BarberApp.Tests;

public class LicenciaDateHelperTests
{
    private static readonly DateTime Hoy = new(2026, 6, 19);

    [Fact]
    public void Detecta_vencida()
    {
        Assert.True(LicenciaDateHelper.EstaVencida("2026-06-18", Hoy));
        Assert.False(LicenciaDateHelper.EstaVencida("2026-06-19", Hoy));
        Assert.False(LicenciaDateHelper.EstaVencida("2026-07-01", Hoy));
    }

    [Fact]
    public void Dias_restantes_y_aviso()
    {
        Assert.Equal(5, LicenciaDateHelper.DiasRestantes("2026-06-24", Hoy));
        Assert.True(LicenciaDateHelper.DebeMostrarAviso("2026-06-24", Hoy));
        Assert.False(LicenciaDateHelper.DebeMostrarAviso("2026-12-31", Hoy));
    }

    [Fact]
    public void Fecha_valida_para_hoy()
    {
        Assert.True(LicenciaDateHelper.EsFechaValida("2026-06-19", Hoy));
        Assert.False(LicenciaDateHelper.EsFechaValida("2026-06-18", Hoy));
    }
}
