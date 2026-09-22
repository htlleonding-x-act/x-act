namespace XActBackend.Core.Util;

public sealed class AuthenticationSettings
{
    public const string SectionKey = "Authentication";

    /// <summary>the realm url the backend uses to fetch the signing keys</summary>
    public required string Authority { get; init; }

    /// <summary>
    /// issuer expected in the token, needed when the backend reaches keycloak under a different host name than
    /// the browser does (keycloak:8080 inside docker, localhost:8080 outside). falls back to the authority
    /// </summary>
    public string? ValidIssuer { get; init; }

    /// <summary>
    /// audience expected in the token. keycloak only stamps a usable one once the client has an audience mapper,
    /// so a realm without that mapper leaves this unset and the audience is not validated
    /// </summary>
    public string? ValidAudience { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;
}
