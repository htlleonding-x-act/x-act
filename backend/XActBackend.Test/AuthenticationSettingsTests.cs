using AwesomeAssertions;
using XActBackend.Core.Util;

namespace XActBackend.Test;

public sealed class AuthenticationSettingsTests
{
    private static readonly AuthenticationSettings Settings = new()
    {
        Authority = "http://keycloak:8080/realms/xact"
    };

    [Theory]
    [InlineData("http://keycloak:8080/realms/xact")]
    [InlineData("http://localhost:8000/realms/xact")]
    [InlineData("https://laptop.example.ts.net/realms/xact")]
    public void IsRealmIssuer_AcceptsRealmUnderAnyHost(string issuer)
    {
        Settings.IsRealmIssuer(issuer).Should().BeTrue();
    }

    [Theory]
    [InlineData("http://keycloak:8080/realms/master")]
    [InlineData("http://keycloak:8080/realms/xact-other")]
    [InlineData("http://keycloak:8080/realms/xact/sub")]
    [InlineData("/realms/xact")]
    [InlineData("")]
    public void IsRealmIssuer_RejectsOtherRealmsAndRelativeIssuers(string issuer)
    {
        Settings.IsRealmIssuer(issuer).Should().BeFalse();
    }
}
