---
name: github-issue
description: Creates GitHub issues for this repository with the GitHub CLI, written in the team's title and description style, and creates the linked working branch for an issue. Use when the user wants to open, file or draft an issue (feature, bug, refactor, chore, docs), or wants to start work on an issue by creating or checking out its branch. Does not open pull requests (see github-pull-request) and does not commit.
compatibility: Requires git and an authenticated GitHub CLI (gh) with write access to the repository.
---

# GitHub Issue

Create issues that read like the ones the team already writes, and give each
issue a branch that is **linked** to it on GitHub, so the issue's Development
panel shows the work in progress.

If any `gh` or `git` command fails, stop and follow the `gh-cli-doctor` skill
before retrying. Do not work around a failure (e.g. by creating a branch with
plain `git` when `gh issue develop` failed) — that silently loses the link.

## Repository facts

- Repo: `htlleonding-x-act/x-act`. Integration branch: **`dev`** (all work
  branches start from it and PRs target it). `main` is the default/release branch.
- Issues and PRs are written in **English**.
- Existing labels: `bug`, `enhancement`, `documentation`, `User Story`,
  `question`, `help wanted`, `good first issue`, `duplicate`, `invalid`,
  `wontfix`. Never invent a label; if unsure, run `gh label list`.
- No milestones are used. Projects need an extra token scope, so only add to a
  project when the user asks.

Re-check these facts with `gh repo view --json nameWithOwner` and
`gh label list` if something looks off; they may drift.

## Before writing anything

1. Make sure the issue does not already exist:
   ```sh
   gh issue list --state all --search "<key words>" --limit 10
   ```
   If one matches, show it to the user instead of creating a duplicate.
2. Know enough to write it. If the user gave one vague line, read the relevant
   code so the description names real screens, services, endpoints or files.
   Ask only for what you cannot find out (e.g. expected behaviour of a bug).

## Issue type

Pick one type; it drives the label, the title style and the branch prefix.

| Type | Use for | Label | Branch prefix |
| :--- | :--- | :--- | :--- |
| feature | new behaviour | `enhancement` | `feat/` |
| bug | something broken | `bug` | `fix/` |
| hotfix | urgent breakage that must ship fast | `bug` | `hotfix/` |
| refactor | restructure, no behaviour change | — | `refactor/` |
| docs | documentation only | `documentation` | `docs/` |
| chore | tooling, deps, config | — | `chore/` |
| ci | pipelines | — | `ci/` |
| test | tests only | — | `test/` |

Add `User Story` only when the user asks for a user story.

## Title

- Short, specific, sentence case, no trailing period, under ~70 characters.
- **Feature / refactor / chore / docs:** imperative verb first.
  `Add share button and QR code for game codes`,
  `Implement kick-voting for players`.
- **Bug:** describe the symptom and where it happens, not the fix.
  `Game screen stuck on "Acquiring GPS…" after start`.
- No type prefixes like `[Feature]`, `Bug:`, or `feat:` — the label carries the
  type, and conventional prefixes belong to commits and PR titles.

## Description

Use Markdown with `##` headings. Keep it as long as the problem needs: a typo
fix is three lines, a cross-cutting feature can have a design section. Omit a
section instead of filling it with "N/A".

**Feature / refactor / chore / docs**

```markdown
## Summary
What should change and why — 1–3 sentences from the user's / player's view.

## Context
Current state, relevant files, screens, endpoints (optional).

## Tasks
- [ ] Backend: …
- [ ] Frontend: …

## Acceptance criteria
- [ ] Observable result that proves it is done
```

**Bug / hotfix**

```markdown
## Description
What goes wrong, in one or two sentences.

## Steps to reproduce
1. …
2. …

## Expected behaviour
…

## Actual behaviour
…

## Environment
Device / OS / app build, if known.

## Notes
Suspected cause or related code, if known (optional).
```

Rules:
- Reference code as `path/to/file.dart` and other issues as `#123`.
- Only state facts you know. Mark guesses as such ("probably caused by …").
- Split work that has separate acceptance criteria into separate issues; use
  `--parent <number>` for sub-issues when the user wants a parent/child split.

## Create the issue

Creating an issue is visible to the whole team. **Show the user the title,
labels, assignee and body first and create only after they agree**, unless they
already said to just create it.

Write the body to a temp file to avoid shell-quoting problems, then:

```sh
gh issue create \
  --title "<title>" \
  --body-file <tmp-file> \
  --label <label>            # omit when the type has no label
  # --assignee @me           # only when the user asks or will work on it now
```

The command prints the issue URL; the number is its last path segment. Report
the URL to the user.

## Create the linked branch

Do this when the user wants to start working on an issue (new or existing).

### Branch name

```
<prefix>/<issue-number>-<slug>
```

- Prefix from the type table above. For an existing issue, infer the type from
  its labels and title; ask if it is ambiguous.
- Slug: 2–5 words from the title, lowercase, kebab-case, ASCII only
  (`ä→ae`, `ö→oe`, `ü→ue`, `ß→ss`), no filler words, at most ~40 characters.
- Example: issue #84 "Add Docker Compose setup for backend and Keycloak" →
  `feat/84-docker-compose-setup`.

### Pre-flight

```sh
gh issue view <n> --json number,title,state,labels,assignees
gh issue develop --list <n>
git status --porcelain
git fetch origin
```

- Issue closed → ask before creating a branch for it.
- A branch is already linked → offer to check that one out instead of creating
  a second one.
- Uncommitted changes → tell the user; switching branches would carry them
  along. Let them commit or stash first (do not stash on your own).
- Branch name already exists locally or on `origin` → pick a different slug or
  use the existing branch, after asking.

### Create, link and check out

```sh
gh issue develop <n> --base dev --name <branch> --checkout
```

This creates the branch on GitHub from `origin/dev`, links it to the issue,
checks it out locally, and records `dev` as the merge base so a later
`gh pr create` targets `dev` by default.

Then assign the issue to the user if nobody is assigned yet (they are now
working on it):

```sh
gh issue edit <n> --add-assignee @me
```

### Verify

```sh
gh issue develop --list <n>     # branch listed
git branch --show-current       # equals <branch>
```

Report the issue URL and the branch name.
