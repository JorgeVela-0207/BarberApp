using BarberApp.Core;

using Xunit;

namespace BarberApp.Tests;

public class PasswordHelperTests
{
    [Fact]
    public void Hash_y_Verify_coinciden()
    {
        var hash = PasswordHelper.Hash("secret123");
        Assert.True(PasswordHelper.Verify("secret123", hash));
        Assert.False(PasswordHelper.Verify("wrong", hash));
    }

    [Fact]
    public void EsHash_detecta_formato()
    {
        var hash = PasswordHelper.Hash("x");
        Assert.True(PasswordHelper.EsHash(hash));
        Assert.False(PasswordHelper.EsHash("plain"));
    }
}
