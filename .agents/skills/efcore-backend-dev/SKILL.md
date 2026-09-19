---
name: efcore-backend-dev
description: Conventions for writing clean, maintainable C# / .NET backends using ASP.NET Core and EF Core. Use when writing or reviewing an ASP.NET Core / EF Core backend in C#. Builds on the csharp skill — apply both. NOT applicable to non-.NET backends (Rust, Go, Node/TS, Python, Java/Spring, etc.) or to frontend code — skip this skill entirely for those.
---

# ASP.NET Core + EF Core Backend Conventions

House style for C# backends built on **ASP.NET Core** and **EF Core**. These are
concepts and habits, not framework trivia. Apply them when writing or reviewing
such a backend; ignore this skill for any other language or stack. Language-level
C# conventions live in the `csharp` skill — apply it alongside this one.

When a rule and a deadline collide, ship the version that compiles and stays
correct over the "pure" version that doesn't — but mark every such compromise as
debt and pay it back.

## Before you write: design boundaries

For decisions where several constraints interact — which arms a `OneOf` result
needs so callers can branch exhaustively, where the transaction boundary sits
when one service calls another, how an aggregate splits across repositories
without leaking `IQueryable`, what a migration does to existing rows — think the
tradeoffs through before committing to a signature or generating a migration.
Routine CRUD is straightforward; a signature half the codebase depends on is not.

## 1. Layer by responsibility; dependencies point inward only

```
API (Controllers, DTOs, validation, HTTP)  →  Core (Services: business rules)
        →  Persistence (Repositories, UnitOfWork, DbContext, migrations)
        ↘  Shared (constants, clock, extensions)  ↙
```

The API layer knows HTTP and never SQL; Persistence knows storage and never HTTP;
Core knows *rules* and reaches persistence only through interfaces. An entity must
never carry an HTTP status; a controller must never write a query. Keep
import/seed/tooling in its own project so one-off data concerns stay off the
request path. Let the compiler enforce the arrows.

## 2. Program against interfaces; inject everything

Every service, repository, and unit of work is an `interface` with one
`internal sealed` implementation. Use constructor injection (primary constructors
keep it terse); never `new` up a collaborator with behavior — `new` is for data,
DI is for collaborators. Choose lifetimes deliberately (singleton clock/config,
scoped for anything touching the request's `DbContext`). `sealed` by default.

## 3. Model outcomes as data, not exceptions

Expected, recoverable outcomes ("not found", "already exists", "missing items")
are part of a method's contract — model them as a discriminated union
(`OneOf<Success<T>, NotFound, Conflict, …>`). Exceptions are for the *unexpected*
(dropped connection, bug, violated invariant) — never for control flow.

The signature should tell the whole story: a reader who sees
`OneOf<Success<Order>, NotFound, EmployeeAlreadyOrderedToday>` knows every branch
without opening the body. Define failure cases as small `record struct`s nested in
the interface they belong to, carrying the data the caller needs (e.g. the
offending id).

### Never `AsT0` / `IsT0` — handle the union exhaustively

Once an outcome is a union, handle it with `Match` (to produce a value) or
`Switch` (to act). `AsTn`/`IsTn` reach into one arm by position and throw away the
exhaustiveness the union exists to provide: add a case later and `Match`/`Switch`
sites won't compile until handled, while `AsT0` sites silently keep "working" and
become wrong.

> **Never, absolutely never reach for `AsT0`/`IsT0`** (or `.Value` on the wrong
> arm). The only justified exception is when there is genuinely no other way to
> express the code — then prefer *running with `AsT0` over not running at all*,
> but comment why and treat it as debt to remove. In practice it is almost always
> avoidable.

```csharp
// Good — adding a case breaks the build until handled
return result.Match<ActionResult>(
    success  => Ok(Dto.From(success.Value)),
    notFound => NotFound());

// Bad — positional access, silently wrong the day a third case appears
if (result.IsT0) { return Ok(Dto.From(result.AsT0.Value)); }
return NotFound();
```

## 4. Keep controllers thin; rules live in Core

A controller is a fixed pipeline: **validate input → open transaction → delegate
to a service → `Match` the result to an HTTP response → commit or roll back**. No
business logic. Validate shape and trivial bounds at the edge (enum defined, id
non-negative, page size allowed) and reject early with the right 4xx; validate
*rules* in Core. Declare every status you actually return with
`ProducesResponseType`. When every controller follows the same skeleton, a reader
learns one and knows them all.

## 5. Separate the wire from the domain (DTOs)

Never accept or return entities directly. Requests/responses are their own
`record` types. Inbound DTOs are untrusted shapes to validate then map; outbound
DTOs expose only what a client should see and decouple the API from the schema.
Put mapping *with the DTO* as static factory methods (`FromOrder`, `ToEntity`).
Shape response DTOs for the consumer (pre-computed counts, grouped views).

## 6. Validation is declarative and lives with the type

Use FluentValidation, one validator per request type as a nested class on the DTO.
Rules read as specifications (`NotEmpty`, `GreaterThan(0)`, `IsInEnum`,
`GreaterThanOrEqualTo(today)`); inject what a rule needs (e.g. the clock) instead
of reading ambient state. Compose validators for nested collections. Centralize
the *invocation* (a base-controller helper) so every endpoint validates and
formats errors the same way.

## 7. Persistence: Repository + Unit of Work, explicit transactions

Repositories own queries for one aggregate and return entities or purpose-built
result records — never leak `IQueryable` upward. Build queries in steps (filter →
sort → page) and count before paging. The Unit of Work is the single seam for
`SaveChanges` and transaction lifecycle; expose the transaction API
(`Begin/Commit/Rollback`) as its *own* interface (interface segregation) so a
controller coordinating transactions isn't handed the repositories too. Make
boundaries symmetric: every `Begin` has a `Commit` on success and `Rollback` on
every failure path, and `Dispose` rolls back a still-open transaction as a safety
net. Repositories accept the narrowest dependency they need (a `DbSet<T>`, the
clock), not the whole context.

## 8. EF Core: configure explicitly, query deliberately

Configure the model in `OnModelCreating` with small named per-entity methods — be
explicit about keys, relationships, delete behavior, and inheritance strategy, and
remove conventions you don't want. Load only what you need (`Include` behind a
flag, project into result records when you don't need tracking). Do filtering,
sorting, and paging *in the query* so the database does the work. Treat migrations
as reviewed, append-only history: generate, read before applying, never hand-edit
applied ones.

## 9. Determinism: inject the clock, ban ambient state

Never call `DateTime.Now/UtcNow` or `Guid.NewGuid()` in logic. Depend on an
injected clock (and a real temporal library — e.g. NodaTime — over raw
`DateTime`). Pin the timezone in one shared constant. "Is this in the past?" must
be testable without waiting for tomorrow.

## 10. Log with structure and intent

Structured logging with named placeholders (`"Order {id} not found", id`) — never
interpolate into the message. Level matches meaning: `Information` for the normal
story, `Warning` for expected-but-notable (not found, conflict), `Error` (with the
exception object) for the unexpected. Log the decision, not a play-by-play. A
single exception-handling middleware is the backstop that logs unhandled errors
once and returns a clean 500.

## 11. Testing: unit-test rules, integration-test the seams

Unit-test Core services with collaborators substituted (NSubstitute), asserting on
the *union arm returned*, not just a value. Integration-test the real HTTP + EF
stack against a real-ish database reset to a known seed before each test so runs
repeat even if interrupted. Inject a controllable clock so time-dependent rules
are deterministic. xUnit + NSubstitute, plain `Assert`.
