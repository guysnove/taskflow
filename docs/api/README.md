# TaskFlow JSON API

Course 2, Chapter 23. For people writing programs that use TaskFlow. How the API is designed, and why:
[guidelines.md](guidelines.md). The machine-readable description: `GET /openapi/v1.json` (Development),
generated from the code and its `///` comments, so it can't drift from what the server does.

## Authentication

The API uses the same sign-in as the web pages: a cookie.

1. Sign in through `/account/signin` (or register at `/account/register`) and keep the cookie.
2. Every request that changes something (POST, PUT, DELETE) must also send an anti-forgery token in the
   `X-CSRF-TOKEN` header. Read it from any TaskFlow page (the hidden `__RequestVerificationToken` field), e.g. `/tasks/quick`.
   Without it: 400 Bad Request.
3. You only ever see your own tasks. Someone else's task answers 404, exactly like a task that doesn't exist.

Limits: 20 requests per 10 seconds per user (then 429 Too Many Requests, with `Retry-After`).

## Endpoints

| Method and path | Does | Success | Errors |
|---|---|---|---|
| `GET /api/tasks` | list your tasks; optional `done`, `page`, `pageSize` | 200; `X-Total-Count`, `Link` | 400 |
| `GET /api/tasks/{id}` | one task | 200 | 404 |
| `POST /api/tasks` | create; optional `Idempotency-Key` header | 201 + `Location` | 400, 422 |
| `PUT /api/tasks/{id}` | replace title, priority, due, notes, project; include `version` | 200 | 400, 404, 409 |
| `POST /api/tasks/{id}/done` | mark done | 204 | 404 |
| `DELETE /api/tasks/{id}` | delete | 204 | 404 |
| `GET /api/tasks/{id}/steps` | the task's checklist | 200 | 404 |
| `POST /api/tasks/{id}/steps` | add a step: `{ "text": "…" }` | 201 | 400, 404 |
| `POST /api/tasks/{id}/steps/{stepId}/toggle` | tick or untick | 200 | 404 |
| `DELETE /api/tasks/{id}/steps/{stepId}` | remove a step | 204 | 404 |

Errors use one shape, `application/problem+json` (RFC 9457): `type`, `title`, `status`, and `errors` for invalid input.

## Examples (real responses)

These were produced by `stage/api_examples.sh` in the course materials: a fresh Development database
(with its sample tasks), one registered user, and `curl`. Security headers and dates are left out.

### Create a task: 201 Created, with Location
```http
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://127.0.0.1:5023/api/tasks/5

{
  "id": 5,
  "title": "Send the invoice to Acme",
  "priority": "High",
  "due": "2026-12-01",
  "done": false,
  "notes": null,
  "projectId": null,
  "projectName": null,
  "createdOn": "2026-11-22",
  "version": 0
}
```

### List your tasks (paged)
```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
X-Total-Count: 5
Link: </api/tasks?page=2&pageSize=2>; rel="next"

[
  {
    "id": 3,
    "title": "Write report",
    "priority": "High",
    "due": "2026-11-20",
    "done": false,
    "notes": null,
    "projectId": 2,
    "projectName": "Quarterly report",
    "createdOn": "2026-11-22",
    "version": 0
  },
  {
    "id": 4,
    "title": "Call supplier",
    "priority": "High",
    "due": "2026-11-23",
    "done": false,
    "notes": null,
    "projectId": 1,
    "projectName": "Office move",
    "createdOn": "2026-11-22",
    "version": 0
  }
]
```

### Get one task
```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{
  "id": 1,
  "title": "Renew licence",
  "priority": "Medium",
  "due": "2026-11-27",
  "done": false,
  "notes": null,
  "projectId": null,
  "projectName": null,
  "createdOn": "2026-11-22",
  "version": 0
}
```

### Invalid input: 400, with the reasons
```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json; charset=utf-8

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": [
      "Please give the task a title.",
      "A title needs 3 to 80 characters."
    ],
    "Priority": [
      "Please choose High, Medium or Low."
    ]
  },
  "traceId": "00-610fc980…"
}
```

### Someone else's task, or no task: 404 (we never say which)
```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json; charset=utf-8

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-94fbe893…"
}
```

### A stale version: 409 Conflict
```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json; charset=utf-8

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "The task was changed by someone else.",
  "status": 409,
  "detail": "Fetch it again, apply your change, and send it with the new Version.",
  "traceId": "00-902b9e8e…"
}
```

### Mark it done: 204
```http
HTTP/1.1 204 No Content
```

### Without the X-CSRF-TOKEN header
```http
HTTP/1.1 400 Bad Request
```
