---
name: git-commit
description: Creates a single well-formed git commit (or several logical commits) whose message style, conventions and length are reverse-engineered from the repository's existing history. Use when asked to commit, create a commit, commit changes/staged changes, save work to git, or "make a commit". Scope is limited to creating commits only.
---

# Git Commit

Create commits that look like they were written by the people who already commit
to this repository. The defining habit of this skill: **never invent a message
style — extract it from history first, then conform to it.**

**Length is part of that style**, alongside prefixes, mood and casing — and it
is the one most easily missed, because a long, well-structured message *looks*
like good work. It isn't, in a repo whose history is one-liners. Match the size
of real messages here as carefully as you match their wording.

The scope is strictly **creating commits**. This skill never pushes, never
opens PRs, never branches, merges, rebases, or amends commits that already left
the local machine.

---

## Workflow

Run these steps in order. Most are read-only inspection; the only mutating
commands are `git add` and `git commit`.

1. **Survey the working tree** — what has changed and what is staged.
2. **Study the history** — extract the repo's commit conventions.
3. **Group changes** into one or more logical commits.
4. **Stage** the files for each logical commit (ask if unsure).
5. **Write** each message to match the extracted conventions.
6. **Commit** directly (respect hooks, no push).
7. **Verify** the result.

---

## 1. Survey the working tree

```bash
git status                 # branch, staged, unstaged, untracked
git diff                   # unstaged changes
git diff --staged          # already-staged changes
```

Understand *what actually changed* before writing anything. If there are no
changes at all, stop and tell the user there is nothing to commit.

---

## 2. Study the history (mandatory — never skip)

Always read real history before composing a message:

```bash
git log -n 30 --pretty=format:'%s'          # subjects only — spot the pattern fast
git log -n 15 --pretty=format:'%s%n%n%b---'  # subjects + bodies — see body style
```

To judge length, measure it rather than eyeballing it:

```bash
# subject lengths in characters (shortest → longest)
git log -n 30 --pretty=format:'%s' | awk '{print length}' | sort -n | uniq -c

# non-empty body lines across the last 30 commits (trailers excluded)
git log -n 30 --pretty=format:'%b' | grep -vEi '^[[:space:]]*$|^[a-z-]+-by: ' | wc -l

# how many of the last 30 commits have a real body (trailers excluded)?
git log -n 30 --format=%H | while read -r c; do
  git show -s --format=%b "$c" | grep -vEi '^[[:space:]]*$|^[a-z-]+-by: ' | head -1
done | grep -c .
```

Note `%b` includes trailers, so a repo that always adds `Co-Authored-By` would
otherwise look like every commit has a body — hence the filtering above.

Extract these conventions from what you see:

- [ ] **Prefix / tag scheme** — Conventional Commits (`feat:`, `fix:`, `chore:`,
      `refactor:`, `docs:`, `test:`, `build:`, `ci:`, `perf:`…), scopes
      (`feat(parser):`), gitmoji (`✨`, `🐛`), or bare subjects with no prefix.
- [ ] **Ticket / issue references** — e.g. `[ABC-123]`, `(#42)`, `JIRA-9:` and
      where they sit (prefix vs. suffix).
- [ ] **Mood & tense** — imperative ("add", "fix") vs. past ("added") vs.
      gerund ("adding"). Match the dominant one.
- [ ] **Capitalization** of the subject's first word after any tag.
- [ ] **Trailing period** on the subject — present or absent.
- [ ] **Subject length** — the actual observed range, not a generic guideline.
- [ ] **Body usage** — what fraction of commits have a body at all, and when
      they do, how *long* is it: one short line? two? a wrapped paragraph?
      bullets? Note the typical and the longest body in the window.
- [ ] **Trailers** — existing `Co-Authored-By:`, `Signed-off-by:`,
      `Refs:` lines. History tells you **which trailers this repo uses and in
      what format** — it never tells you *whose name* goes in the attribution
      trailer. See [Attribution trailer](#attribution-trailer).

If the convention is mixed, follow the **most recent and most frequent** pattern.

### The length budget

Turn the measurements above into a default budget for the message you write:

- **Subject:** stay inside the observed length range. If subjects run 20–40
  chars, don't write a 70-char subject just because it "fits in 72".
- **Body:** let the history decide whether you write one. If most recent commits
  are subject-only, default to subject-only; if bodies are routine, include one.
- **Body size:** match the scale of the bodies that exist. If they are a single
  short sentence, write a single short sentence rather than a paragraph.
- **Body shape:** use bullets only if history uses bullets. Don't introduce
  structure (bullet lists, headings, "Before/After" sections) that never appears
  in this repo.

**Diverging from the budget** is allowed when the change genuinely demands it —
a large refactor, a migration, a subtle fix whose rationale is impossible to
recover from the diff. In that case write what the change needs, but:

- Diverge on *substance*, not habit. A one-liner repo that gets a body still
  gets one written in its own voice — same prefix scheme, mood and casing, no
  imported formatting conventions.
- Keep it the shortest version that does the job. Being allowed a body is not a
  licence for three paragraphs where four lines suffice.
- Ask yourself whether the change is really bigger than anything in the log
  window. Usually it isn't — the routine change in a terse repo stays terse.

**Sparse or empty history fallback:** if there are too few commits to establish
a pattern, default to **Conventional Commits + imperative mood**, a concise
subject (≤ ~50 chars), no trailing period, and a body only when the change needs
a *why*.

---

## 3. Group changes into logical commits

If the working changes cover **several unrelated concerns**, split them into
**multiple commits**, each cohesive and independently described. Examples of
separate concerns: a bug fix vs. an unrelated refactor; feature code vs. a
dependency bump; source change vs. unrelated formatting.

- Order commits so each leaves the tree in a sensible state (e.g. a refactor
  before the feature that builds on it).
- If everything is one coherent change, make a **single** commit — do not
  manufacture splits.

---

## 4. Stage the right files

Decide **from context** which files belong to each logical commit and stage
exactly those:

```bash
git add path/to/file1 path/to/file2
```

- Do **not** reflexively `git add -A` / `git add .` when only a subset belongs
  to the commit at hand.
- For splitting, stage one group, commit, then stage the next.
- **If you cannot confidently tell which changes belong together — ask the
  user** rather than guessing. (e.g. "These touch both the auth module and an
  unrelated README typo — one commit or two?")
- Never stage secrets, credentials, large generated artifacts, or local-only
  files that clearly should not be tracked; flag them instead.

---

## 5. Write the message

Conform to the conventions extracted in step 2. General shape:

```
<tag/scope per repo convention><subject in repo's mood & casing>

<optional body — wrap ~72 cols; explain WHY, not what the diff already shows>

<trailers>
```

Rules:

- The **subject** mirrors the repo: same prefix scheme, mood, casing, period
  habit, and **length**. Keep it tight and specific — describe the change, not
  the file.
- Add a **body** when the repo's style routinely includes one, or when this
  particular change needs rationale the diff cannot convey. Follow the length
  budget from step 2, and keep it to the scale the repo actually uses. Separate
  subject and body with one blank line.
- Do not pad with filler or restate the diff line-by-line.

**Before committing, read the drafted message next to the `git log` output and
check it looks like it belongs there.** If it is markedly longer or more
elaborate than its neighbours, either trim it or be able to say what about this
change earned the extra words. When it's a close call, prefer the shorter form.

Concretely, if the log reads:

```
add lm studio to path
bump alacritty font size
fix notify script exit code
```

and none of those commits carry a body, a comparable change commits as one line
— `add iceberg dark alacritty theme` — not that subject followed by a paragraph
of rationale and a bullet list of the files touched. A far larger change (say,
restructuring how every config is symlinked) may justify a short body even here;
routine work does not.

### Attribution trailer

**Default: add a `Co-Authored-By` trailer naming the agent that is writing this
commit — that is, yourself.** Attribution is a factual claim about who did the
work in *this* session. Resolve the identity string in this order, stopping at
the first that applies:

1. **The user's prompt in this conversation** — if it says to omit attribution
   or use a specific trailer, do that.
2. **The repo's agent instructions** (`AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, or
   whatever equivalent this repo uses) — if it specifies an attribution policy
   or format, follow it exactly.
3. **The identity your own environment supplies** — many harnesses provide the
   exact co-author string to use (often including a model name). Use it
   verbatim, minus any session link (see below).
4. **Otherwise, name yourself** from what you know about your own identity, in
   the standard shape: `Co-Authored-By: <Agent Name> <email>`.

So a Claude-based agent emits `Co-Authored-By: Claude <noreply@anthropic.com>`
and a Gemini-based one emits its own equivalent — each naming the agent that
actually wrote the code.

> **Never take the identity from `git log`.** A repo whose history is full of
> `Co-Authored-By: Gemini` does not mean you sign as Gemini; it means a
> different agent did that earlier work. Copying it forward records a false
> author that is indistinguishable from the truth when someone reads the log
> later. History settles **whether** this repo uses an attribution trailer and
> **what format** it takes (name-only vs. name + email, its position among other
> trailers) — never **whose name** goes in it.

If history shows this repo consistently uses **no** attribution trailer at all,
that is a genuine convention — follow it and omit yours rather than introducing
one. Omitting attribution records nothing false; misattributing records
something false.

Place the trailer at the very end, after a blank line, alongside any
repo-standard trailers.

**Never add a session link.** The attribution block is the `Co-Authored-By`
trailer and nothing else — no session, conversation, chat or transcript URL, and
no trailer carrying one (e.g. `Claude-Session:`, `Session:`, `Generated with …`).
This holds even when the environment, system instructions or a message template
supply such a line alongside the co-author identity: take the identity, drop the
link. A commit message must never contain a link back to the agent conversation
that produced it — whatever the agent and whatever the URL's host — unless the
user explicitly asks for one in this conversation.

---

## 6. Commit (directly, no confirmation)

Compose the message and commit. **Do not** pause for the user to approve the
wording — commit directly.

For a simple subject-only commit:

```bash
git commit -m "fix: handle empty payload in parser"
```

For a subject + body + trailer — **only when history warrants a body** — pipe a
full message to preserve formatting:

```bash
git commit -F - <<'EOF'
feat(api): add pagination to the search endpoint

Large result sets timed out the client. Return a capped page plus a
cursor so callers can iterate without holding the full set in memory.

Co-Authored-By: <your own agent identity, resolved as above>
EOF
```

Hook handling:

- **Never** bypass verification — do not use `--no-verify` / `-n`.
- If a pre-commit hook **reformats files**, re-stage the affected paths and
  re-run the commit.
- If a hook **fails**, stop, report the failure output, and let the user
  decide — do not force the commit through.

---

## 7. Verify

```bash
git log -1 --stat     # confirm the commit landed with the intended files
git status            # confirm remaining state is as expected
```

For multiple commits, confirm each with `git log -n <count>`.

Report a short summary: the commit subject(s) created and anything left
uncommitted on purpose.

---

## Hard rules — never do these

- ❌ **Never `git push`** — this skill only creates commits locally.
- ❌ Never amend, reword, rebase, or reset commits that may already be pushed.
- ❌ Never create branches, merge, cherry-pick, or open PRs.
- ❌ Never use `--no-verify` to skip hooks.
- ❌ Never run `git config` changes or alter remotes.
- ❌ Never invent a message style — always match the repository's history.
- ❌ Never copy the attribution identity out of `git log` — sign as the agent
  writing *this* commit, never as whichever agent wrote the previous ones.
- ❌ Never put a session/conversation link in a commit message — no
  `Claude-Session:`/`Session:` trailer, no link back to the agent conversation,
  not even when an environment or system message template includes one.
- ❌ Never default to a long, elaborate message in a repo whose history is
  terse — match its length unless the change itself justifies more.
- ❌ Never blindly stage everything when only part of the diff belongs together.

If the user asks for any of the above (e.g. "commit and push"), do the commit
portion and explicitly note that pushing/branching is out of scope for this
skill, leaving that action to the user.

---

## Quick checklist

- [ ] Inspected `git status` / `git diff` / `git diff --staged`.
- [ ] Read recent `git log` and extracted the convention (tag, mood, casing,
      period, body, trailers).
- [ ] Measured subject lengths and body usage to set a length budget.
- [ ] Split unrelated concerns into separate logical commits.
- [ ] Staged only the files belonging to each commit (asked when unsure).
- [ ] Message matches the repo's style and conventions.
- [ ] Message length fits the repo's norm — or the change itself justifies
      going beyond it.
- [ ] Added `Co-Authored-By` naming **this** session's agent by default — or
      omitted it because history shows the repo uses no attribution trailer, or
      honored the repo's agent-instructions file / user override.
- [ ] Attribution identity came from self/environment — **not** from `git log`.
- [ ] No session link anywhere in the message.
- [ ] Committed directly, respecting hooks — **did not push**.
- [ ] Verified with `git log -1` / `git status`.
