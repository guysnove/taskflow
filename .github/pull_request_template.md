<!-- Course 2, Chapters 19 and 21: what a reviewer needs. Delete what doesn't apply. How we review: CONTRIBUTING.md §3. -->

## What and why

<!-- One or two sentences. Link the requirement (FR-…) or issue. -->

## How it was tested

- [ ] New behaviour has tests, written before or with the code (unit tests for rules, integration tests for the request-to-database path)
- [ ] I saw each new test **fail** once (red) before it passed
- [ ] Bug fix: a test that fails without the fix
- [ ] `dotnet test` passes locally; the E2E test too if pages changed (`test: end-to-end` task)
- [ ] `./tools/coverage.sh` passes (the quality gate)

## For the reviewer

- [ ] Names say what things do; no commented-out code
- [ ] Tests check behaviour (results, stored rows), not implementation details
- [ ] Docs updated if behaviour changed (`docs/requirements`, `docs/testing.md`, API guidelines)

## Size and history (Course 2, Chapter 21)

- [ ] One change, small enough to review in about 20 minutes (under ~400 changed lines)
- [ ] Up to date with `main`; fixups squashed; commit subjects follow CONTRIBUTING.md §2
