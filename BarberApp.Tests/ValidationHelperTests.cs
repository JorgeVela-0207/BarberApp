using BarberApp.Core;
using Xunit;

namespace BarberApp.Tests;

public class ValidationHelperTests
{
    [Theory]
    [InlineData("5512345678", true)]
    [InlineData("", true)]
    [InlineData("123", false)]
    public void Telefono_valido(string tel, bool esperado) =>
        Assert.Equal(esperado, ValidationHelper.EsTelefonoValido(tel));

    [Fact]
    public void Normalizar_telefono()
    {
        Assert.Equal("5512345678", ValidationHelper.NormalizarTelefono("+52 55 1234 5678"));
    }

    [Theory]
    [InlineData("150", true, 150)]
    [InlineData("-1", false, 0)]
    public void Precio_valido(string texto, bool ok, decimal valor)
    {
        Assert.Equal(ok, ValidationHelper.EsPrecioValido(texto, out var p));
        if (ok) Assert.Equal(valor, p);
    }
}
