namespace XActBackend.Core.Util;

public sealed class AuthenticationSettings
{
    public const string SectionKey = "Authentication";

    /// <summary>the realm url the backend uses to fetch the signing keys</summary>
    public required string Authority { get; init; }

    /// <summary>
    /// issuers accepted in the token. keycloak puts the host name the client reached it under into the issuer, so
    /// this lists every such host (localhost:8080 on the dev machine, 10.0.2.2:8080 from the android emulator, the
    /// lan address from a phone) whenever the backend reaches keycloak under another one, like keycloak:8080 inside
    /// docker. falls back to the authority
    /// </summary>
    public string[]? ValidIssuers { get; init; }

    /// <summary>
    /// audience expected in the token. keycloak only stamps a usable one once the client has an audience mapper,
    /// so a realm without that mapper leaves this unset and the audience is not validated
    /// </summary>
    public string? ValidAudience { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;
}
