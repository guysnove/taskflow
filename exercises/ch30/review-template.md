# Engineering review: <system> <version>

- **Date:** · **Reviewers:** · **Scope:** <branch/commit>
- **Method:** every claim checked against the running system or the code. Automated checks: <script>. A first user's path: <smoke test>.
- **Verdict:** <ready / ready with conditions / not ready> — for <intended use: users, load, data>.

## 1. Automated checks
<paste the output of your review script: build, format, tests, coverage, requirements traced, docs links, vulnerable packages, complexity>

## 2. The running system
| Check | Result |
|---|---|
| Deploy | |
| Smoke test after deploy | |
| A bad release: does it roll back? | |
| Container: builds, non-root, size, smoke test | |
| Performance (warm; conditions stated) | |
| Observability: health, logs, metrics, traces; an alert fired on purpose | |

## 3. Area by area
| # | Area | What the system has | Evidence (a command, a test, a document) |
|---|---|---|---|
| 1 | Requirements | | |
| 2 | Code quality | | |
| 3 | Architecture | | |
| 4 | Project structure | | |
| 5 | Services | | |
| 6 | SOLID | | |
| 7 | Patterns, where useful | | |
| 8 | Database access | | |
| 9 | API contracts | | |
| 10 | Tests | | |
| 11 | Security | | |
| 12 | Caching | | |
| 13 | Performance | | |
| 14 | Logging | | |
| 15 | CI/CD | | |
| 16 | Containers | | |
| 17 | Deployment | | |
| 18 | Verifying the running app | | |
| 19 | Documentation | | |
| 20 | Operations | | |

## 4. Known limits and risks (accepted)
| Limit | Why it's acceptable now | When to revisit |
|---|---|---|

## 5. Decisions taken in this review
-
