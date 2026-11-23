# Chapter 30 — the capstone kit

The tools TaskFlow 2.0 was reviewed with, to reuse on your own project:

| File | What it is | Run it |
|---|---|---|
| `review.sh` | the automated half of an engineering review: build with warnings as errors, format, tests, end-to-end tests, coverage gate, requirements traced, docs links, vulnerable packages, complexity, debt, ADRs — one line of evidence each | from a TaskFlow checkout: `bash tools/review.sh` |
| `smoke.sh` | a first user's path against a running TaskFlow (health, register, create, API, complete, anonymous refused) | `bash tools/smoke.sh http://127.0.0.1:5040` |
| `review-template.md` | the review document's structure: automated checks, the running system, 20 areas, accepted limits, decisions | copy it into your project's docs |

TaskFlow's own copies live in `tools/`; its filled-in review is `docs/review/2026-11-23-taskflow-2.0.md`.
For the browser test, `review.sh` needs Playwright's Chromium (`pwsh TaskFlow.Tests/bin/Debug/net10.0/playwright.ps1 install chromium`),
or `TASKFLOW_BROWSER=<path to chrome>`.
