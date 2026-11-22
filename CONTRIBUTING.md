# Contributing to TaskFlow

Course 2, Chapter 21. How we work together on this repository. Short on purpose: if a rule here gets in
the way, open a pull request that changes it.

## 1. The workflow: GitHub Flow, plus release branches

```
main ──●────●──────────●────●─────────●──   always releasable; CI green; protected
        \   ↑ squash    \   ↑          \
         ●──●            ●──●           ●──●   feature/…  (hours to a few days)
                                  release/1.4 ──●── v1.4.1   only fixes, cherry-picked from main
```

0. Pick the top **Ready** story on the board (docs/planning/README.md); one story = one branch = one pull request.
1. `git switch main && git pull`, then `git switch -c feature/<short-name>` (or `fix/<bug-number>-<name>`, `docs/<name>`).
2. Small commits, each one building and passing its tests. Push early: `git push -u origin feature/<name>`.
3. Open a **pull request** to `main` when it's ready for review (or a *draft* PR to show work in progress).
   Fill in the template. One PR = one change a reviewer can understand in about 20 minutes (under ~400 changed lines).
4. CI must be green. At least one approving review (see `.github/CODEOWNERS` for who reviews what).
5. **Squash and merge**, with a good title. Delete the branch.
   On a team repository, protect `main` with a ruleset (Settings → Rules → Rulesets): require a pull request,
   one approval and the `build-and-test` check. (This course's repository doesn't: each chapter's milestone is
   published directly, so that `milestone/chNN` branches match the videos.)
6. Keep your branch up to date with `git pull --rebase origin main` **while it's only yours**; after someone else
   has based work on it, merge `main` into it instead. Never force-push `main` or a shared branch;
   on your own branch use `git push --force-with-lease`, never plain `--force`.

**Releases.** When we release, we tag `main` (`v1.4.0`) and, if the release needs fixes later, cut
`release/1.4` from the tag. A fix goes to `main` first, with a test, then `git cherry-pick -x` to the release
branch, which gets a new tag (`v1.4.1`). Nothing is developed on a release branch.

## 2. Commit messages

```
Refuse task titles with control characters (F1)        ← subject: imperative, ≤ 72 characters, no full stop

The development console logger wrote titles as they were, so a line   ← body: WHY, wrapped at ~72
break could forge a log entry. Validate at the boundary instead.

Refs: FR-LIST-2, docs/security/threat-model.md               ← requirement, issue, or document
```

- Write the subject as a command: "Add…", "Fix…", "Refuse…" (it completes "If applied, this commit will…").
- One logical change per commit. Formatting-only changes in their own commit.
- `tools/hooks/commit-msg` checks the subject. Turn it on once per clone: `git config core.hooksPath tools/hooks`.
- Tidy your own branch before review: `git commit --fixup <sha>`, then `git rebase -i --autosquash origin/main`.

## 3. Reviewing

The goal is better code **and** a team that keeps wanting to review each other's work.

- Review within one working day. Small PRs get fast reviews; that's why we keep them small.
- Read the description and the tests first, then the code.
- Label each comment, so the author knows what's required:

| Label | Meaning | Example |
|---|---|---|
| **blocking:** | must change before merge (a bug, a security issue, a broken rule) | "blocking: Ben can see Ana's tasks here; the query isn't filtered by user." |
| **question:** | I don't understand; explain (in the code, if the answer helps the next reader) | "question: why is `today` passed in rather than read from the clock?" |
| **suggestion:** | I'd do it differently; your call | "suggestion: `IsOverdue(today)` on TaskItem would make this read better." |
| **nit:** | tiny, optional (naming, wording) | "nit: 'overdue' rather than 'over due'." |
| **praise:** | say what's good, specifically | "praise: the boundary test for 'due today' is exactly the case I'd have missed." |

- Comment on the **code**, not the person: "this query misses…", not "you forgot…". Ask, don't command.
- Explain *why*, and link to the rule (`docs/design`, `docs/testing.md`, the threat model).
- Prefer a test that shows the problem over a paragraph describing it.
- Approve when it's better than what's on `main` and has no blocking issue. Perfect is not the bar.
- As the author: reply to every comment (a fix, an answer, or "let's do that in a follow-up: #12"), and say thanks.

## 4. Before you push

```bash
dotnet build -warnaserror && dotnet format --verify-no-changes && dotnet test --filter "Category!=Growth&Category!=E2E"
```

## 5. Security

Never commit secrets (see `SECURITY.md` and `docs/security/threat-model.md`). Report vulnerabilities privately.
