# Definitions of Ready and Done

Course 2, Chapter 22. Agreed by the team; changed only in a retrospective.

## Definition of Ready (before a story enters a sprint)

- [ ] Written as a user story: **As a** …, **I want** …, **so that** … — and linked to a requirement id (FR-…).
- [ ] Acceptance criteria as examples (**Given / When / Then**), including at least one "no" case (what must *not* happen).
- [ ] Estimated, and **8 points or fewer** (otherwise: split it).
- [ ] Dependencies known (another story, a decision, a person).
- [ ] Security and privacy questions asked (threat model, `docs/security/threat-model.md`).

## Definition of Done (before a story counts as done)

- [ ] Every acceptance criterion has an automated test, written first, that passes.
- [ ] `dotnet build -warnaserror`: 0 warnings. `dotnet format --verify-no-changes`: clean.
- [ ] All tests pass in CI, including the end-to-end test and the coverage gate (`tools/coverage.sh`).
- [ ] Reviewed and approved in a pull request (CONTRIBUTING.md §3); squash-merged to `main`.
- [ ] Requirement traceability updated (`docs/requirements/current.md`, `[Trait("Req", …)]`).
- [ ] Docs updated where behaviour changed (README, API contract, threat model, ADR if a decision was made).
- [ ] Shown to the product owner at the sprint review.

"Done" means all of these. "Done except the tests" is **not done**: it goes back to In progress, and its points don't count.
