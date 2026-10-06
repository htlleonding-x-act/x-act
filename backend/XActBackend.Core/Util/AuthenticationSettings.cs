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
    /// accepts any issuer that names the authority's realm, whatever host it carries, in place of
    /// <see cref="ValidIssuers"/>. meant for a dev stack that clients reach under host names nobody knows in
    /// advance, like a tailnet name. the signing keys still come from the authority, so only that keycloak can
    /// issue a token that passes
    /// </summary>
    public bool AcceptAnyIssuerHost { get; init; }

    /// <summary>
    /// audience expected in the token. keycloak only stamps a usable one once the client has an audience mapper,
    /// so a realm without that mapper leaves this unset and the audience is not validated
    /// </summary>
    public string? ValidAudience { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;

    // the scheme check matters on unix, where a bare path like /realms/xact parses as an absolute file uri
    public bool IsRealmIssuer(string issuer) =>
        Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri)
        && (issuerUri.Scheme == Uri.UriSchemeHttp || issuerUri.Scheme == Uri.UriSchemeHttps)
        && issuerUri.AbsolutePath == new Uri(Authority).AbsolutePath;
}
