---
description: "Use when fetching data from the web, CI logs, artifact URLs, signed URLs, Azure Blob URLs, or simple API/text responses. Prefer curl in the terminal over webpage fetch tools for raw data retrieval."
---

# Web Fetching

- Prefer `curl` in the terminal for raw remote content such as CI logs, artifact downloads, signed URLs, plain-text endpoints, and JSON APIs.
- Use webpage/content fetch tools only when the goal is to summarize or inspect rendered page content rather than retrieve the exact response body.
- For expiring, authenticated, or redirecting URLs, default to `curl -L -s` and then pipe to `head`, `grep`, `sed`, or `jq` as needed.
- When debugging remote responses, keep the raw output in the terminal path and filter locally instead of depending on fetch tools.
