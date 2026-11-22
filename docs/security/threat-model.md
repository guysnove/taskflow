# TaskFlow — threat model and security review

Course 2, Chapter 20. Reviewed against `milestone/ch19`; fixes in `milestone/ch20`.
Method: draw the system and its trust boundaries, go through STRIDE for each boundary,
then test each threat (`TaskFlow.Tests/Integration/SecurityReviewTests.cs`, `[Trait("Category", "Security")]`).

## 1. What we protect

| Asset | Why it matters |
|---|---|
| Task titles, notes, project names | private: people write their lives into a task list |
| Accounts (email, password hash) | taking over an account exposes everything above |
| The sign-in and anti-forgery keys (`DataProtection:KeysPath`) | whoever has them can forge sign-in cookies |
| SMTP credentials | send email as TaskFlow: phishing that looks real |
| Availability | a to-do list nobody can open is useless |

## 2. The system and its trust boundaries

```
 Browser / API client ──(1) internet, HTTPS──▶ nginx ──(2) localhost──▶ TaskFlow (ASP.NET Core)
                                                                         │      │        │
                                                       (3) file system ──┘      │        └──(5) SMTP server (internet)
                                                       SQLite db, keys, logs    │
                                                                    (4) background email worker
```

A **trust boundary** is a line that data crosses from something we don't control to something we do. Every value crossing it is checked on the inside.

1. **Internet → TaskFlow:** everything a client sends: forms, JSON, headers, cookies, the URL.
2. **nginx → TaskFlow:** `X-Forwarded-For/Proto` are trusted only from localhost (Course 1, Ch. 29).
3. **TaskFlow → disk:** the database, the key ring and the logs. Logs leave the server (log tools, backups, support staff).
4. **Request → background worker:** email content built from user data.
5. **TaskFlow → mail server:** credentials, and recipients' data.

## 3. STRIDE

| Threat | Example against TaskFlow | Mitigation | Test |
|---|---|---|---|
| **S**poofing | guess or steal a password; forge a cookie | PBKDF2 hashing, lockout, sign-in rate limit (Course 1); keys outside the web root | AccountService/SignInRules tests, RateLimitTests |
| **T**ampering | change someone else's task (IDOR); cross-site POST (CSRF); SQL injection | every query filtered by the signed-in user; anti-forgery on forms and cookie-authenticated API; EF Core parameters | `BrokenAccessControl_…`, `ATaskWithoutAToken_…`, `SqlInjection_…` |
| **R**epudiation | "I never deleted that" | log *who* did *what* by id, with time | — (accepted; see §5) |
| **I**nformation disclosure | stored XSS; open redirect; **private text in logs**; secrets in git | Razor encoding + CSP; `Url.IsLocalUrl`; **F2**; **F4** | `StoredXss_…`, `OpenRedirect_…`, `CreatingATask_DoesNotWriteItsTitleToTheLogs`, `NoSecretsInRepoTests` |
| **D**enial of service | flood the API; lock out someone else's account | rate limits; 5-minute lockout (not permanent) | RateLimitTests |
| **E**levation of privilege | reach /admin as a normal user; mass assignment (`Done`, `UserId`) | `[Authorize(Roles = "Admin")]`; input DTOs without those fields | `TheAdminPage_IsForAdminsOnly` |

## 4. Findings (Chapter 20) and what we did

| # | Finding | Severity | Fix | Commit |
|---|---|---|---|---|
| F1 | Titles, names and step texts accepted **control characters** (line breaks, escape codes) through the API or a hand-made form post. With the development console logger, a title could **forge log lines**; in email subjects it throws. | Medium | `[NoControlCharacters]` on one-line texts; notes and descriptions allow line breaks only | s2 |
| F2 | **Private user text in logs**: task titles and project names at Information level; full email addresses on mail errors. One unit test even *required* the full address. | Medium | log ids, never user text; `LogSafe.Email` → `a***@example.com` | s3, s4 |
| F3 | The CI step `dotnet list package --vulnerable` **never fails** (exit code 0 even with findings). The real protection was the build's `-warnaserror`, in CI only. | Low (no vulnerable package today) | `NU1901–NU1904` are errors in `Directory.Build.props`: every restore, every machine | s5 |
| F4 | SMTP had **no way to sign in**, so real deployments would have had to hack it in, likely into a settings file. | Medium (future) | `Email:Username/Password` from user-secrets or environment / `EnvironmentFile`; a test that settings files hold no secrets | s6 |

## 5. Risks we accept (for now), and why

| Risk | Why accepted | Revisit when |
|---|---|---|
| **Account enumeration at registration** ("There is already an account with this email address.") | Sign-in gives the same answer for "no such email" and "wrong password"; registration is rate-limited to 5/minute/IP. Hiding it fully needs email verification (send "someone tried to register with your address"), which TaskFlow doesn't have yet. | email verification is added |
| **Lockout as denial of service** (5 wrong passwords lock anyone's account for 5 minutes) | short and self-healing; the sign-in rate limit slows an attacker | many accounts are targeted at once |
| **Sign-in cookies can't be revoked** before they expire (14 days, sliding) | no "sign out everywhere" or password change yet | a password-change feature is added (use a security stamp) |
| **Repudiation:** no audit log of who changed what | a personal to-do list; logs have ids and times | teams share tasks (`docs/requirements/notifications-and-teams.md`) |

## 6. How to keep it this way

- New endpoint? Write the "someone else's id" test first (Chapter 19). Run `dotnet test --filter Category=Security`.
- New log line? Ids and counts only.
- New setting that is a secret? Read it from configuration; put it in user-secrets / the environment; `NoSecretsInRepoTests` guards the files.
- Report a vulnerability: see `SECURITY.md`.
