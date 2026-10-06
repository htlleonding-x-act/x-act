---
name: github-pull-request
description: Opens a GitHub pull request for the current branch (usually an issue's branch) with the GitHub CLI, with a title and description in the team's style, and pushes the branch if needed. Use when the user wants to open, create or draft a PR, or "submit" the work on an issue. Does not create commits (see git-commit), issues or branches (see github-issue), and does not merge PRs.
compatibility: Requires git and an authenticated GitHub CLI (gh) with write access to the repository.
---

# GitHub Pull Request

Open a PR from the current branch into `dev` whose title and description match
what the team already writes, and tie it to its issue.

If any `gh` or `git` command fails, stop and follow the `gh-cli-doctor` skill
before retrying.

## Repository facts

- Repo: `htlleonding-x-act/x-act`. PRs target **`dev`**, never `main`, unless
  the user explicitly asks for a release PR (`dev` → `main`).
- Branches look like `<type>/<issue-number>-<slug>` (older ones:
  `<type>/<slug>`). See the `github-issue` skill.
- PR titles follow Conventional Commits, like the commit history:
  `feat: …`, `fix(lobby): …`, `refactor: …`, `chore(agents): …`.
- PRs and issues are written in **English**.

## 1. Pre-flight

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

## 2. Find the issue

- Take the number from the branch name (`feat/84-docker-compose-setup` → `84`)
  and confirm the link: `gh issue develop --list 84` shows the branch.
- No number in the branch name → ask the user which issue it belongs to, or
  whether there is none. Do not guess from similar titles.
- Read the issue to reuse its wording and acceptance criteria:
  ```sh
  gh issue view <n> --json title,body,labels
  ```

## 3. Write the title

`<type>(<scope>)?: <summary>`

- `type` = branch prefix (`feat`, `fix`, `refactor`, `docs`, `chore`, `ci`,
  `test`; a `hotfix/` branch uses `fix`).
- `scope` is optional; use it when the change sits in one area
  (`backend`, `lobby`, `chat`, `realtime`, `location`, `migrations`, …).
  Check `git log --format=%s -30 origin/dev` for scopes already in use.
- Summary: lowercase start, imperative, no period, under ~70 characters, says
  what the PR does — not the issue title copied verbatim.

Examples: `feat: add share button and QR code for game codes`,
`fix(location): stop the acquiring GPS loop on game start`.

## 4. Write the description

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

## 5. Confirm, push, create

Opening a PR notifies the team. **Show the user the title, base branch, labels
and body first and create only after they agree**, unless they already said to
just open it.

Push the branch if it has no upstream or is ahead of it:

```sh
git push -u origin HEAD
```

Never force-push. If the push is rejected, report it and use `gh-cli-doctor` /
ask the user.

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

## 6. Verify and report

```sh
gh pr view --json url,baseRefName,headRefName,title,isDraft
```

Check `baseRefName` is `dev`. Report the PR URL and remind the user that the
issue stays open until someone closes it after the merge, e.g.
`gh issue close <n> --reason completed` — only run that when the user asks.
