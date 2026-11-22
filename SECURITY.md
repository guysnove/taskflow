# Security policy

TaskFlow is a teaching application, but we treat security reports seriously.

## Reporting a vulnerability

Please **don't open a public issue**. Use GitHub's private vulnerability reporting
("Security" tab → "Report a vulnerability"), with:

- what an attacker can do, and the steps to reproduce it;
- the version (branch or commit) you tested.

We aim to reply within 7 days. Thank you for helping keep TaskFlow's users safe.

## What we do

- Our threat model and accepted risks: `docs/security/threat-model.md` (Course 2, Chapter 20).
- Security tests: `dotnet test --filter Category=Security`.
- Vulnerable packages fail the build (`Directory.Build.props`).
