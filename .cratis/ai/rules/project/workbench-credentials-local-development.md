---
applyTo: "**/*"
---

## Workbench credentials (local development)

The Workbench requires signing in before any view loads — unauthenticated requests to
`/.cratis/me` and `/api/event-stores` return `401` and the app redirects to `/login`.

| Username | Password |
| --- | --- |
| `Admin` | `ChangeMeNow!` |

These are the local development defaults only. They are not valid anywhere else, and nothing
outside a developer machine should ever accept them.
