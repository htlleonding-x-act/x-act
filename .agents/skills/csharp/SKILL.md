---
name: csharp
description: House style and conventions for writing clean C# code of any kind. Use when writing, editing, refactoring, or reviewing C# code — libraries, console apps, backends, tests, scripts. NOT applicable to other languages (F#, VB.NET, Java, TypeScript, etc.) — skip this skill entirely for those.
---

# C# Conventions

House style for **C#**, whatever the code is part of. Language-level habits only.

## Before you write: check target framework

Read the project's `TargetFramework` and `LangVersion` from the `.csproj` to know
which language features, BCL APIs, and package versions apply. The conventions
below are house style and version-independent.

## 1. Brace every control-flow body

Every `if`, `else`, `for`, `foreach`, `while`, `do`, `using` and `lock` body gets
braces, even a single statement. A braced one-liner is fine; so is a ternary when
the point is choosing between two values.

```csharp
// Good
if (order is null) { return NotFound(); }

if (order is null)
{
    return NotFound();
}

var label = isActive ? "active" : "inactive";

// Bad — no braces; the next statement added under it silently runs unconditionally
if (order is null) return NotFound();

while (queue.TryDequeue(out var job))
    Run(job);
```

## 2. Never use `continue`

`continue` is a jump back to the loop head: to know which statements run, a reader
has to scan the whole body for it. Restructure with branches instead — invert the
skip condition and put the work inside the `if`, and turn several skip cases into
`if`/`else if`/`else`.

```csharp
// Good — every path is visible in the branches
foreach (var line in lines)
{
    if (!string.IsNullOrWhiteSpace(line))
    {
        if (line.StartsWith('#'))
        {
            comments++;
        }
        else
        {
            entries.Add(Parse(line));
        }
    }
}

// Bad
foreach (var line in lines)
{
    if (string.IsNullOrWhiteSpace(line)) { continue; }
    if (line.StartsWith('#')) { comments++; continue; }
    entries.Add(Parse(line));
}
```

When the branches nest too deep, extract the loop body into a well-named method —
never bring `continue` back to flatten it.

## 3. General habits

- `sealed` by default; inheritance only on purpose.
- Make illegal states unrepresentable: `required` members, nullable reference
  types taken seriously, enums validated at the boundary with `Enum.IsDefined`.
- Centralize constants and config; bind config to typed settings once at startup.
  No magic strings or numbers.
- Small, single-purpose methods.
- `async`/`await` end-to-end with cancellation tokens; never `.Result` or `.Wait()`.
