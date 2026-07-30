# Chronicle — Project-Specific Instructions

Project-local context for this repository. See `.claude/CLAUDE.md` for the shared Cratis corpus.

## Running the local stack

The Workbench is a Vite dev server that proxies to the Chronicle Kernel:

| Piece | Address | Notes |
| --- | --- | --- |
| Workbench (dev) | `http://localhost:9000` | `yarn dev` from `Source/Workbench` |
| Chronicle Kernel | `https://localhost:35000` | **HTTPS** with a dev certificate — use `curl -k`; plain `http://` returns an empty reply |
| Orleans gateway / silo | `127.0.0.1:30000` / `127.0.0.1:11111` | |
| MongoDB | `localhost:27017` | Runs in Docker |

The kernel serves everything on the single TLS port; the Vite proxy sets `secure: false` for it.

## Workbench credentials (local development)

The Workbench requires signing in before any view loads — unauthenticated requests to
`/.cratis/me` and `/api/event-stores` return `401` and the app redirects to `/login`.

| Username | Password |
| --- | --- |
| `Admin` | `ChangeMeNow!` |

These are the local development defaults only. They are not valid anywhere else, and nothing
outside a developer machine should ever accept them.

## Verifying Workbench behavior

Drive the Workbench through the browser tooling rather than guessing from source — several of its
views are fed by observable (SSE) queries whose behavior only shows up at runtime:

- Sign in first, or every view renders empty and looks like a data bug.
- Inspect the SSE frames when a live view misbehaves. Observable queries default to `Delta`
  transfer mode, so a frame carries a `changeSet` (`added` / `replaced` / `removed`) and an empty
  `data` array — the client reconstructs the collection from the previous state.
- Delta reconciliation matches items by a property named `id` on both the server
  (`ChangeSetComputor`) and the client (`useObservableQuery`). A read model without an `id`
  falls back to whole-payload JSON equality, which is fragile for models carrying a changing
  timestamp. Check this first when a live list grows, duplicates, or fails to drop rows.
