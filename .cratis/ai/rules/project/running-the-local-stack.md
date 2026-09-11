---
applyTo: "**/*"
---

## Running the local stack

The Workbench is a Vite dev server that proxies to the Chronicle Kernel:

| Piece | Address | Notes |
| --- | --- | --- |
| Workbench (dev) | `http://localhost:9000` | `yarn dev` from `Source/Workbench` |
| Chronicle Kernel | `https://localhost:35000` | **HTTPS** with a dev certificate — use `curl -k`; plain `http://` returns an empty reply |
| Orleans gateway / silo | `127.0.0.1:30000` / `127.0.0.1:11111` | |
| MongoDB | `localhost:27017` | Runs in Docker |

The kernel serves everything on the single TLS port; the Vite proxy sets `secure: false` for it.
