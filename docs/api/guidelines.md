# TaskFlow API guidelines

Course 2, Chapter 15. How TaskFlow's JSON API is designed, so every endpoint behaves the same way.
The contract itself is `/openapi/v1.json` (Development); its readable summary is checked by
`ApiContractTests` against `TaskFlow.Tests/Integration/Approved/ApiContract.approved.txt`.

## 1. Resources and addresses

| Rule | Example |
|---|---|
| Nouns, plural, lower case | `/api/tasks`, `/api/tasks/3/steps` |
| A child resource lives under its parent | `/api/tasks/3/steps/7` |
| An action that isn't create/replace/delete is a sub-resource, with POST | `POST /api/tasks/3/done`, `POST /api/tasks/3/steps/7/toggle` |
| Ids are numbers; a non-number is "no such resource" | `/api/tasks/abc` → 404 |

## 2. Methods and status codes

| Method | Meaning | Success | Safe? | Idempotent? |
|---|---|---|---|---|
| GET | read | 200 | yes | yes |
| POST (create) | add a resource | 201 + `Location` | no | **with an `Idempotency-Key`** |
| POST (action) | do something | 204, or 200 with the new state | no | `done` yes; `toggle` no |
| PUT | replace the editable fields; body includes `version` | 200 | no | yes |
| DELETE | remove | 204; then 404 | no | yes (the state is the same) |

Errors: 400 invalid input · 401 not signed in · 404 not found (or not yours — we never say which) ·
409 changed by someone else (stale `version`) · 415 not JSON · 422 `Idempotency-Key` reused for a different request · 429 too many requests (`Retry-After`).

## 3. Bodies

- Requests and responses are DTOs (`TaskDto`, `StepDto`, `TaskInput`): never database entities. No `UserId`, no navigation properties.
- JSON property names are camelCase. Dates are `yyyy-MM-dd`.
- Every error is a **problem details** document (RFC 9457), `application/problem+json`, with `type`, `title`, `status`, `traceId`, and `errors` for validation. (`ApiErrorTests`.)

## 4. Lists

`GET /api/tasks?done=false&projectId=2&page=1&pageSize=20`

- Every parameter is optional. **Without any, the answer is every task**, as in version 1 from the start.
- `pageSize` is 1–100 (default 20 when paging). The order is total (… then `id`), so pages never overlap.
- `X-Total-Count`: how many tasks match. `Link: <…page=3…>; rel="next"` when there is a next page.

## 5. Changing the API without breaking programs

Safe (additive): a new endpoint; a new **optional** parameter; a new response field; a new error status that was always possible.
Breaking: removing or renaming anything; a new **required** parameter or field; changing a type or a meaning; changing the default.

Breaking changes need a new version (`/api/v2/...`) next to the old one, announced with a date, and the old version keeps working until then.
`ApiContractTests` makes every change visible in review: read the diff of the approved file before approving.

## 6. Retries

`POST /api/tasks` accepts an `Idempotency-Key` header (any unique string, up to 100 characters, e.g. a GUID).
Repeating the same request with the same key returns the first answer (`Idempotent-Replayed: true`) and creates nothing.
Saved answers are kept in `IdempotencyRecords` (TD-11: no clean-up yet).
