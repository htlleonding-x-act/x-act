---
name: github-workflow
description: Handles this repository's GitHub workflow with the GitHub CLI — creates issues in the team's title and description style, creates and checks out the branch linked to an issue, opens pull requests into dev, and diagnoses failing gh or git push commands. Use when the user wants to open, file or draft an issue, start work on an issue, open or draft a PR or "submit" the work on an issue, or when a gh or git push command errors or the user asks whether gh is set up. Does not create commits (see git-commit) and does not merge PRs.
compatibility: Requires git and an authenticated GitHub CLI (gh) with write access to the repository. Network access to github.com.
---

# GitHub Workflow

Create issues and pull requests that read like the ones the team already
writes, and give each issue a branch that is **linked** to it on GitHub, so the
issue's Development panel shows the work in progress.

## When a command fails

If any `gh` or `git` command fails, stop and read
[`references/troubleshooting.md`](references/troubleshooting.md) before
retrying. Read it too when the user asks whether gh is set up. Do not work
around a failure (e.g. by creating a branch with plain `git` when
`gh issue develop` failed) — that silently loses the link.

## Repository facts

- Repo: `htlleonding-x-act/x-act`. Integration branch: **`dev`** (all work
  branches start from it and PRs target it). `main` is the default/release
  branch; PRs go into `main` only when the user explicitly asks for a release
  PR (`dev` → `main`).
- Issues and PRs are written in **English**.
- Existing labels: `bug`, `enhancement`, `documentation`, `User Story`,
  `question`, `help wanted`, `good first issue`, `duplicate`, `invalid`,
  `wontfix`. Never invent a label; if unsure, run `gh label list`.
- No milestones are used. Projects need an extra token scope, so only add to a
  project when the user asks.

Re-check these facts with `gh repo view --json nameWithOwner` and
`gh label list` if something looks off; they may drift.

## Types

The type drives the issue label, the issue title style, the branch prefix and
the PR title type.

| Type | Use for | Label | Branch prefix | PR type |
| :--- | :--- | :--- | :--- | :--- |
| feature | new behaviour | `enhancement` | `feat/` | `feat` |
| bug | something broken | `bug` | `fix/` | `fix` |
| hotfix | urgent breakage that must ship fast | `bug` | `hotfix/` | `fix` |
| refactor | restructure, no behaviour change | — | `refactor/` | `refactor` |
| docs | documentation only | `documentation` | `docs/` | `docs` |
| chore | tooling, deps, config | — | `chore/` | `chore` |
| ci | pipelines | — | `ci/` | `ci` |
| test | tests only | — | `test/` | `test` |

Add `User Story` only when the user asks for a user story.

## Issues

### Before writing anything

1. Make sure the issue does not already exist:
   ```sh
   gh issue list --state all --search "<key words>" --limit 10
   ```
   If one matches, show it to the user instead of creating a duplicate.
2. Know enough to write it. If the user gave one vague line, read the relevant
   code so the description names real screens, services, endpoints or files.
   Ask only for what you cannot find out (e.g. expected behaviour of a bug).

### Title

- Short, specific, sentence case, no trailing period, under ~70 characters.
- **Feature / refactor / chore / docs:** imperative verb first.
  `Add share button and QR code for game codes`,
  `Implement kick-voting for players`.
- **Bug:** describe the symptom and where it happens, not the fix.
  `Game screen stuck on "Acquiring GPS…" after start`.
- No type prefixes like `[Feature]`, `Bug:`, or `feat:` — the label carries the
  type, and conventional prefixes belong to commits and PR titles.

### Description

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

### Create the issue

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

## Linked branch

Do this when the user wants to start working on an issue (new or existing).

### Branch name

```
<prefix>/<issue-number>-<slug>
```

- Prefix from the [type table](#types). For an existing issue, infer the type
  from its labels and title; ask if it is ambiguous.
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

## Pull requests

Open a PR from the current branch into `dev` and tie it to its issue.

### 1. Pre-flight

```sh
git branch --show-current
git status --porcelain
git fetch origin
git log --oneline origin/dev..HEAD
gh pr view --json url,state 2>/dev/null
```

Stop and tell the user when:
- The current branch is `dev` or `main` — a PR needs a work branch.
- There are uncommitted changes — they would not be in the PR. Offer to commit
  them first with the `git-commit` skill; never commit as a side effect.
- `origin/dev..HEAD` is empty — nothing to merge.
- A PR for this branch already exists — give its URL; offer to update its
  title/body with `gh pr edit` instead of opening a second one.

If the branch is behind `origin/dev` and conflicts are likely, mention it;
do not rebase or merge on your own.

### 2. Find the issue

- Take the number from the branch name (`feat/84-docker-compose-setup` → `84`)
  and confirm the link: `gh issue develop --list 84` shows the branch.
  Older branches look like `<type>/<slug>` without a number.
- No number in the branch name → ask the user which issue it belongs to, or
  whether there is none. Do not guess from similar titles.
- Read the issue to reuse its wording and acceptance criteria:
  ```sh
  gh issue view <n> --json title,body,labels
  ```

### 3. Write the title

PR titles follow Conventional Commits, like the commit history:
`<type>(<scope>)?: <summary>`

- `type` from the [type table](#types), matching the branch prefix.
- `scope` is optional; use it when the change sits in one area
  (`backend`, `lobby`, `chat`, `realtime`, `location`, `migrations`, …).
  Check `git log --format=%s -30 origin/dev` for scopes already in use.
- Summary: lowercase start, imperative, no period, under ~70 characters, says
  what the PR does — not the issue title copied verbatim.

Examples: `feat: add share button and QR code for game codes`,
`fix(location): stop the acquiring GPS loop on game start`.

### 4. Write the description

Base it on the actual diff (`git diff origin/dev...HEAD --stat` and the
commits), not only on the issue. Keep it proportional: a one-line fix gets a
two-line summary.

```markdown
## Summary
What this PR changes and why, 1–3 sentences.

## Changes
**Backend**
- …

**Frontend**
- …

## Testing
- How it was verified: tests run, manual steps on a device, …

Closes #<n>
```

Rules:
- Drop the Backend/Frontend split when only one side changed; drop any section
  that would be empty.
- **Testing lists only what was really done.** If nothing was run, say
  "Not tested yet" — never invent test runs.
- Add `## Notes` for things reviewers must know: migrations to apply, new
  config keys, breaking API changes, stacked PRs ("Merge after #81").
- Add screenshots only when the user provides them (`--attach <file>`).
- `Closes #<n>` names the issue for reviewers and puts a mention in the issue's
  timeline. **It does not link or auto-close the issue**, because GitHub only
  honours closing keywords on PRs into the default branch (`main`), and these
  PRs go into `dev`. The real link comes from the branch that
  `gh issue develop` linked to the issue.

### 5. Confirm, push, create

Opening a PR notifies the team. **Show the user the title, base branch, labels
and body first and create only after they agree**, unless they already said to
just open it.

Push the branch if it has no upstream or is ahead of it:

```sh
git push -u origin HEAD
```

Never force-push without the user's explicit go-ahead. If the push is rejected,
report it and check [`references/troubleshooting.md`](references/troubleshooting.md)
or ask the user.

Then create the PR, writing the body to a temp file:

```sh
gh pr create \
  --base dev \
  --head <branch> \
  --title "<title>" \
  --body-file <tmp-file> \
  --assignee @me
  # --label <label>   # same label as the issue (bug / enhancement / documentation), if any
  # --draft           # when the user says the work is not finished
  # --reviewer <login>  # only when the user names reviewers
```

`--head` stops gh from prompting about where to push; the push above already
happened.

### 6. Verify and report

```sh
gh pr view --json url,baseRefName,headRefName,title,isDraft
```

Check `baseRefName` is `dev`. Report the PR URL and remind the user that the
issue stays open until someone closes it after the merge, e.g.
`gh issue close <n> --reason completed` — only run that when the user asks.
