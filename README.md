# X-ACT

Location-based multiplayer game (Scotland Yard style) with a .NET 10 backend and a
Flutter client.

## Requirements

- .NET 10 SDK
- Flutter 3.41 or newer
- Docker (for the containerised stack and the integration tests)
- [just](https://github.com/casey/just)

## Quick start

```bash
just docker-up          # postgres, keycloak and the backend in containers
just frontend           # flutter client against http://localhost:5200
```

`just docker-up` applies pending EF Core migrations on startup, because the backend
container runs in the Development environment. Production applies them explicitly with
`just db-update`.

Whenever the migration chain changes, drop the database volume instead of migrating an
existing one:

```bash
just docker-reset       # docker compose down -v
just docker-up
```

Run `just` on its own for the full recipe list.

## Authentication

Login goes through Keycloak using the OAuth2 authorization-code flow with PKCE. The
client is public — it ships no secret — so the code exchange is bound to a per-attempt
verifier and the callback is checked against a `state` value.

Logging in is optional by design: the client can still play as a guest, so `POST
/api/auth/register` is the only endpoint that requires a token today.

`just docker-up` imports [`docker/keycloak/xact-realm.json`](docker/keycloak/xact-realm.json)
on first start, which creates the realm `xact` and the public client `x-act-frontend`
with PKCE (`S256`) required and an audience mapper for the backend. No user accounts are
in that file: self-registration is enabled, so create an account from the login page, or
add one in the admin console at <http://localhost:8080> (`admin` / `adminpassword`).

The realm is only imported when it does not exist yet. After changing the export, run
`just docker-reset` to have it applied.

### Running the Flutter web client

The realm registers `http://localhost:8088` as a redirect URI and web origin, so the web
client has to use that port:

```bash
cd xact_frontend && flutter run -d chrome --web-port=8088 --dart-define=API_BASE_URL=http://localhost:5200
```

Desktop builds use a local callback server on port 9482, Android and iOS use the
`xact://login-callback` deep link. Both are registered in the realm export as well.

### Backend configuration

| Key | Required | Notes |
| --- | --- | --- |
| `Authentication:Authority` | yes | Realm URL the backend fetches signing keys from. Startup fails without it. |
| `Authentication:ValidIssuer` | no | Issuer expected in the token. Needed when the backend reaches Keycloak under a different host name than the browser does (`keycloak:8080` inside Docker, `localhost:8080` outside). Falls back to the authority. |
| `Authentication:ValidAudience` | no | Audience expected in the token. Only validated when set, since Keycloak issues a usable audience only once the client has an audience mapper. |
| `Authentication:RequireHttpsMetadata` | no | Defaults to `true`; the development and Docker configs set it to `false`. |
| `General:ClientOrigin` | yes | Allowed CORS origin. An origin ending in `:*` allows any loopback port. |

Environment variables use double underscores, e.g. `Authentication__Authority`.

The credentials in `docker-compose.yaml` are for the local stack only — no deployment
should inherit that file.

## Tests

```bash
cd backend
dotnet test XActBackend.Test/XActBackend.Test.csproj       # unit tests
dotnet test XActBackend.TestInt/XActBackend.TestInt.csproj # integration tests, needs Docker
```

## Reference

- Agent and contributor conventions: [`AGENTS.md`](AGENTS.md)
- Entity relationship diagram: [`backend/erd.puml`](backend/erd.puml)
- Migration helper: [`backend/ManageMigration.ps1`](backend/ManageMigration.ps1)
