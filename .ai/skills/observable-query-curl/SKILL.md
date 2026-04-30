---
name: observable-query-curl
description: Practical guidance for debugging and exploring Cratis Arc observable queries with cURL or other plain HTTP clients. Use this whenever the user mentions observable queries together with cURL, curl, HTTP GET debugging, waiting for the first payload, Server-Sent Events, SSE, streaming JSON, or long polling. Also use it when the user wants to inspect an observable query without writing frontend code.
---

# Debug Observable Queries with cURL

Use this skill to help the user work with observable query endpoints from a terminal.

The main choice is:

1. **Snapshot GET** — return the current observable value once
2. **Wait for first payload** — block until the first payload exists
3. **SSE stream** — keep the connection open and follow updates
4. **Long polling** — repeat blocking HTTP requests instead of keeping one stream open

## Step 1 — Identify the endpoint style

Figure out whether the user has:

- a **model-bound observable query** route
- a **controller-based observable query** route
- the **observable query demultiplexer SSE endpoint**

If the user does not know the route yet, help them find it before suggesting commands.

## Step 2 — Choose the correct cURL workflow

### A. Get the current snapshot

Use a plain `GET` when the user wants the current observable value right now.

```bash
curl "https://localhost:5001/api/orders/observe-all"
```

Explain that this returns the current `QueryResult` snapshot once and then closes.

### B. Wait for the first payload

Use `waitForFirstResult=true` when the observable might not have produced its first value yet.

```bash
curl "https://localhost:5001/api/orders/observe-all?waitForFirstResult=true"
```

If the user wants a custom timeout, add `waitForFirstResultTimeout=<seconds>`.

```bash
curl "https://localhost:5001/api/orders/observe-all?waitForFirstResult=true&waitForFirstResultTimeout=10"
```

Explain that:

- the timeout value is in **seconds**
- the response is still a normal JSON `QueryResult`
- if the observable is not ready and waiting is **not** enabled, the endpoint can return a not-ready response instead of blocking

### C. Stream JSON continuously with SSE

Use SSE when the user wants the observable to keep pushing updates over one connection.

```bash
curl --no-buffer \
  -H "Accept: text/event-stream" \
  "https://localhost:5001/api/orders/observe-all"
```

Explain that:

- Arc sends SSE frames such as `data: {...}`
- each frame contains a serialized `QueryResult`
- `--no-buffer` helps cURL print each event as it arrives

### D. Emulate long polling

Use long polling when the user wants repeated blocking HTTP responses instead of one continuous SSE stream.

```bash
while true; do
  curl --silent \
    "https://localhost:5001/api/orders/observe-all?waitForFirstResult=true&waitForFirstResultTimeout=15"
  echo
done
```

Explain that long polling means:

- each request waits for data or timeout
- the server returns one JSON payload
- the client immediately opens the next request

## Step 3 — Tailor the answer to the user's goal

If the user says:

- **"I want to see the first payload"** → prefer `waitForFirstResult=true`
- **"I want live streaming JSON"** → prefer SSE with `Accept: text/event-stream`
- **"I want long polling"** → prefer a loop that repeatedly calls the HTTP endpoint with `waitForFirstResult=true`
- **"I just need the latest value"** → prefer a plain `GET`

## Step 4 — Mention what the user will receive

Always explain the payload shape:

- snapshot and long-poll requests return a normal JSON `QueryResult`
- SSE returns `data:` frames, each containing a serialized `QueryResult`

If relevant, mention that `QueryResult.Data` holds the full snapshot and `QueryResult.ChangeSet` may also be present for collection updates.

## Response format

When helping the user, prefer this structure:

1. Short recommendation for the correct transport
2. One or two ready-to-run `curl` commands
3. One short note explaining what the response looks like
4. One short note about timeout or retry behavior when relevant

## Examples

**Example 1 — wait for first payload**

User request:

> I need to debug an observable query with curl and the first item is not ready immediately.

Good response shape:

- recommend `waitForFirstResult=true`
- provide the command
- mention `waitForFirstResultTimeout`

**Example 2 — streaming JSON**

User request:

> How do I keep watching an observable query from the terminal?

Good response shape:

- recommend SSE
- provide `curl --no-buffer -H "Accept: text/event-stream" ...`
- explain the `data:` frames

**Example 3 — long polling**

User request:

> I do not want SSE, I want long polling from curl.

Good response shape:

- provide a looped `curl` example
- explain that each response is a normal JSON snapshot
- explain that the client opens the next request after each response
