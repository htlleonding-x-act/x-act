# Troubleshooting gh and git push

Find the **one root cause** that blocks GitHub work, explain it in plain words,
and give the exact fix.

## Rules

- **Diagnose read-only.** Every check below only reads state. Never change auth,
  config or installed packages on your own.
- **Never run interactive auth yourself** (`gh auth login`, `gh auth refresh`,
  `gh auth switch` with a prompt). They open a browser or device-code flow and
  hang in a non-interactive shell. Ask the user to run them in a separate,
  real terminal window. Do not suggest Claude Code's `!` prefix for these: it
  has no TTY, and gh aborts with `--hostname required when not running
  interactively`. Always give the full form with `-h github.com`.
- **Never print tokens.** Check whether `GH_TOKEN`/`GITHUB_TOKEN` is *set*, never
  echo its value. `gh auth status` already masks tokens; do not pass
  `--show-token`.
- **Never install with `sudo` yourself.** Give the install command; the user runs it.
- **Stop at the first failing check.** Later checks depend on earlier ones
  (no auth → every API call fails), so reporting five symptoms of one cause
  only confuses.

## Quick path: read the error first

If a command already failed, match its message before running the full
checklist — most errors name their cause.

| Error text contains | Cause | Fix |
| :--- | :--- | :--- |
| `command not found: gh` / `gh: not found` | gh not installed | [Install](#1-installed) |
| `unknown flag` / `unknown command` | gh too old | Update gh (same package manager as install) |
| `You are not logged into any GitHub hosts` | not logged in | User runs `gh auth login -h github.com` |
| `The token in ... is invalid` / `HTTP 401` / `Bad credentials` | token expired or revoked | User runs `gh auth login -h github.com` again; if `GH_TOKEN` is set, replace or unset it |
| `missing required scopes [X]` / `requires the 'X' scope` | token lacks scope X | User runs `gh auth refresh -h github.com -s X` |
| `SAML` / `SSO` / `Resource protected by organization SAML enforcement` | token not authorized for the org | User runs `gh auth refresh -h github.com`, or authorizes the token for the org on github.com |
| `HTTP 403` + `Resource not accessible by integration` / `must have push access` | no write permission on the repo | Ask a repo admin for write access |
| `HTTP 404` / `Could not resolve to a Repository` | wrong repo resolved, or no read access | [Check 3](#3-repository-resolution) |
| `API rate limit exceeded` | rate limit | Wait for reset (see check 5), or authenticate if unauthenticated |
| `could not create issue: ... label ... not found` | label does not exist | `gh label list`, pick an existing label |
| `a pull request for branch ... already exists` | PR exists | `gh pr view <branch> --json url` and use that PR |
| `No commits between` | branch has nothing to merge | Commit first, or check the base branch |
| `Permission denied (publickey)` on `git push` | SSH key not set up | [Check 6](#6-git-push-transport) |
| `dial tcp`, `i/o timeout`, `could not resolve host`, `TLS handshake` | network / proxy / DNS | [Check 5](#5-network-and-rate-limit) |
| `--hostname required when not running interactively` | interactive auth command run without a TTY (e.g. via an agent shell) | User reruns it in a separate terminal window, with `-h github.com` |

When the message is unclear, rerun the failing command with `GH_DEBUG=api` set
to see the HTTP status and response body.

## Full checklist

Run in order; stop at the first failure.

### 1. Installed

```sh
command -v gh && gh --version
```

Missing → give the install command for the user's OS:

| OS | Command |
| :--- | :--- |
| Arch Linux | `sudo pacman -S github-cli` |
| Debian/Ubuntu | follow the official apt repo steps at https://github.com/cli/cli/blob/trunk/docs/install_linux.md (the distro `gh` package is often very old) |
| Fedora | `sudo dnf install gh` |
| macOS | `brew install gh` |
| Windows | `winget install --id GitHub.cli` |

Then continue with check 2 after the user installed it.

### 2. Authentication

```sh
gh auth status
printenv GH_TOKEN >/dev/null && echo "GH_TOKEN is set"
printenv GITHUB_TOKEN >/dev/null && echo "GITHUB_TOKEN is set"
```

`gh auth status` exits non-zero when no host is logged in or a token is invalid.
Read its output for:

- **Not logged in** → user runs `gh auth login -h github.com` (choose GitHub.com, SSH or HTTPS
  to match the remote URL from check 3).
- **Token invalid** → same, log in again.
- **Env token set** → `GH_TOKEN`/`GITHUB_TOKEN` override the stored login.
  If the env token is the broken one, the user must fix or `unset` it; logging
  in again does nothing while it is set.
- **Wrong active account** (several accounts listed, the active one is not a
  member of the repo's org) → user runs `gh auth switch --user <login>`.
- **Scopes** → the `Token scopes:` line must include `repo` (and `read:org` for
  org repos). Projects need `project` (`read:project` to read).
  Missing → user runs `gh auth refresh -h github.com -s <scope>`. Fine-grained tokens show no
  scope list; check their repo permissions on github.com instead.

### 3. Repository resolution

```sh
git rev-parse --is-inside-work-tree
git remote -v
gh repo view --json nameWithOwner,viewerPermission,hasIssuesEnabled
```

- Not a git repo → `cd` into the repository first.
- No GitHub remote → nothing for gh to target; the user adds one.
- `nameWithOwner` is not the intended repo (forks, several remotes) → user runs
  `gh repo set-default <owner>/<repo>`, or pass `-R <owner>/<repo>` per command.
- `hasIssuesEnabled: false` → issues are disabled in the repo settings.

### 4. Permission

`viewerPermission` from check 3:

- `ADMIN`, `MAINTAIN`, `WRITE` → can create issues, branches and PRs.
- `TRIAGE`, `READ` → can create issues, but cannot push branches or use
  `gh issue develop`. Ask a repo admin for write access.

### 5. Network and rate limit

```sh
gh api rate_limit --jq '.resources.core | "\(.remaining)/\(.limit), resets \(.reset | todate)"'
```

- Connection errors → no network, DNS, VPN, or proxy. If a proxy is required,
  `HTTPS_PROXY` must be set in the shell gh runs in.
- `remaining` is 0 → wait until the reset time shown.

### 6. Git push transport

Only relevant when `git push` fails, since gh's API calls and git's pushes
authenticate separately.

```sh
git remote get-url origin
gh auth status   # see "Git operations protocol"
```

- Remote is `git@github.com:...` (SSH): `ssh -T git@github.com` must print
  `Hi <login>! You've successfully authenticated` (its exit code is 1 even on
  success; read the text). `Permission denied (publickey)` → the user adds an SSH
  key (`gh ssh-key add ~/.ssh/id_ed25519.pub` after `ssh-keygen -t ed25519`) or
  switches the remote to HTTPS.
- Remote is `https://...`: pushes need a credential helper → user runs
  `gh auth setup-git`.
- Push rejected as non-fast-forward → not a gh problem; the branch diverged.
  Report it, never force-push without the user's explicit go-ahead.

## Report

Tell the user, briefly:

1. **What failed** — the command and its error, quoted.
2. **Why** — the root cause from the checks.
3. **Fix** — the exact command, and who runs it (the user for anything
   interactive, sudo, or on github.com).
4. **Next** — what you will retry once it is fixed.

If every check passes but the original command still fails, say so, show the
`GH_DEBUG=api` output of the failing call, and do not guess.
