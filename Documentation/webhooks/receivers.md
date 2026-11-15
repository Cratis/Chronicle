---
title: "Webhook receivers: delivery guarantees, idempotency and security"
description: What a Chronicle webhook request contains, what delivery guarantees a receiver can rely on, how to key duplicate protection, and how to secure the endpoint.
---

A webhook receiver is an HTTP endpoint that the Kernel calls, so it suits a consumer that cannot keep a connection open to Chronicle, such as a function or a container that scales to zero. This page is for the person writing that endpoint. It covers what each request carries, what Chronicle promises about delivery, how to make the receiver safe to call again, and which controls protect it. To register a webhook and tune retries, start with [Webhooks](./index.mdx).

## The guarantee

At-least-once delivery. Ordered per partition, except that after a delivery timeout the previous request may still be executing while the retry starts. Handlers must be idempotent; use the event sequence number or keyed appends.

Chronicle never promises exactly-once delivery to a webhook. Retries, [automatic recovery probes](./index.mdx#automatic-recovery-from-quarantine), manual partition retries and replays can all send an event that the receiver has already handled.

## What a request contains

Chronicle delivers events with `POST` requests to the registered URL. Each request carries one batch as a JSON body (`application/json`, camelCase property names). With [OAuth client credentials](./index.mdx#oauth-client-credentials) or [managed identity](./index.mdx#managed-identity), a `401` response makes the Kernel refresh the token and send the same batch once more, so one delivery attempt can reach your receiver as two requests. The body has two properties:

| Property | Contents |
| --- | --- |
| `partition` | The partition the events belong to, as a string. Observers partition by event source id. |
| `events` | One or more events from that partition, in sequence-number order. Each has a `context` and a `content`. |

The `context` of each event carries the values a receiver needs to recognize it again, among them:

| Property | Contents |
| --- | --- |
| `sequenceNumber` | The event's position in the event sequence. |
| `eventStore` | The event store the event belongs to. |
| `namespace` | The namespace within that event store. |
| `eventSourceId` | The event source the event was appended for. |
| `eventType` | The event type and generation of the delivered content. |
| `occurred` | When the event occurred. This is the append time unless the appending code supplied its own time. |

The `content` is the event itself, released from [compliance](#personal-data-is-delivered-in-plaintext) protection.

The request headers are the ones you configured: the authorization header for the chosen [authentication](#authenticating-the-kernel-to-your-receiver) and any custom headers registered with the webhook. Chronicle adds no request id, delivery id or signature header to webhook requests in this version.

A response in the `2xx` range acknowledges every event in the request. Any other outcome acknowledges none of them, as described next.

## How delivery behaves

**Per partition, in order.** A request carries the events of a single partition. While a partition is failing, Chronicle holds back its later events, and it resumes from the first event that was not acknowledged. A failing partition does not hold back the others, and there is no ordering between partitions. The exception is observer quarantine: when enough partitions are parked, or a response marks a configuration problem, Chronicle pauses the whole webhook. The percentage threshold is measured against partitions that have recorded failures, not against every partition, so a single parked partition can pause the webhook while all other partitions are healthy. See [Receiver back-pressure and permanent failures](./index.mdx#receiver-back-pressure-and-permanent-failures) for the thresholds.

**A failed request is resent as a whole.** Chronicle cannot tell which events in a request the receiver processed before it failed or timed out, so the retry starts again at the first event of that request. If your receiver handled three of five events and then returned `500`, those three arrive again.

**A timeout does not stop your receiver.** `RequestTimeoutSeconds` (60 seconds by default) cancels the Kernel's side of the call. Your receiver may still be running when the retry is sent, so two executions for the same events can overlap. Keep the receiver's own execution limit at or below the Kernel's request timeout, and see [Delivery retries and timeouts](./index.mdx#delivery-retries-and-timeouts) for the schedule.

**The status code chooses the recovery path.** `5xx`, `408`, `429` and connection failures are retried with backoff; `429` and `503` with a valid `Retry-After` defer the partition instead. `410 Gone` parks the partition until an operator intervenes. Any other `4xx`, including `401` and `403` after a token refresh, pauses the whole observer as a configuration problem. Redirects are not followed. The details are in [Receiver back-pressure and permanent failures](./index.mdx#receiver-back-pressure-and-permanent-failures).

That last point matters for a serverless receiver. Answer `503` with a `Retry-After` while it is warming up or overloaded, not `400` or `404`, because a `4xx` pauses all delivery for the webhook rather than just delaying one partition.

**Replay sends history again.** A webhook is replayable unless it was registered with `NotReplayable()`. When it is replayed, Chronicle sends the stored events again under the same sequence numbers.

## Make the receiver safe to call again

### Key on the event, not the request

The durable identity of one delivered event is the combination of:

- the event store,
- the namespace,
- the observer id, which is the webhook id,
- the event sequence the webhook observes (the event log unless you chose another),
- the event's sequence number.

The request body carries the event store, namespace and sequence number. The webhook id and event sequence are fixed for a registration, so the receiver already knows them. If one endpoint serves more than one webhook, give each webhook its own URL path or a custom header so the receiver can tell them apart.

Do not key on anything that describes the request rather than the event. Because a retry resends the whole request starting from the first unacknowledged event, the set of events in a request is not a stable identity.

### Choose the receipt for the effect

How you use that key depends on what the receiver does with the event.

- **A side effect that must happen once**, such as sending an email or calling a payment API. Record a receipt under the event key in the same transaction as the effect where you can, or pass the key to an API that deduplicates requests. Check for the receipt before acting. A replay then sends nothing again, which is usually what you want for an email.
- **State the receiver builds from events**, such as a table or a search index. Write it so that applying the same event twice gives the same result, for example an upsert by the event key or by the entity the event describes. Do not skip an event just because its key was seen before: a replay re-applies history to rebuild that state.

If the receiver must never see history again, register the webhook with `NotReplayable()`. That removes replay, not retries, so the receipt is still required.

### Appending back into Chronicle

When the receiver's effect is appending an event to Chronicle, use a [keyed append](../events/appending.mdx#appending-once). Derive the `IdempotencyKey` from the event key above, so every redelivery of the same event builds the same key and Chronicle writes the event only once. Keys must not contain personal data, and keyed appends need a Kernel that supports them.

## Secure the receiver

### Authenticating the Kernel to your receiver

The Kernel sends the credentials you register with the webhook: basic authentication, a bearer token, an OAuth client-credentials token, or a [managed identity](./index.mdx#managed-identity) token, plus any custom headers. Validate them on every request. [OAuth client credentials](./index.mdx#oauth-client-credentials) and managed identity give you short-lived tokens that your receiver can validate against the identity provider; a static bearer token or password stays valid until you rotate it.

Webhook requests are not signed in this version, so authentication is the only proof that a request came from your Kernel. A receiver that accepts unauthenticated requests, such as a public function URL with no authentication, accepts them from anyone who knows the URL.

### Where the Kernel may send requests

The Kernel checks every registration and delivery against a [target validation policy](./index.mdx#target-validation), and what it enforces depends on the validation mode.

- **HTTPS and host approval.** A plain `http://` URL or a host missing from `AllowedHosts` is refused in `Enforce` mode. The other modes, including the default `BlockLocalAddresses`, only warn. In Development, Chronicle also accepts HTTP for loopback targets or with `AllowInsecureHttp`, and accepts loopback and `host.docker.internal` without approval, even in `Enforce` mode. Outside Development, `AllowInsecureHttp` is ignored.
- **Address checks.** These are separate from HTTPS and host approval. Depending on the mode, a host that is not in `AllowedHosts` and resolves to a local or private address is still refused. Cloud metadata addresses are refused in every mode unless explicitly allowed.

Set the mode to `Enforce` in production, approve your receiver's exact host, and use HTTPS.

### Personal data is delivered in plaintext

The Kernel releases events from compliance protection before it delivers them to any observer, webhooks included. A webhook receiver therefore receives personal data in plaintext, protected in transit only by HTTPS. Erasing a subject later does not reach what the receiver stored. Keep only what you need, and see [Data delivered to webhooks and HTTP receivers](../compliance/erasing-a-subject.mdx#data-delivered-to-webhooks-and-http-receivers) for the erasure procedure. Encrypted or redacted delivery is planned in [#5132](https://github.com/Cratis/Chronicle/issues/5132).

## Related topics

To run a .NET client inside the function that appends and reads events, see [Serverless mode](../clients/dotnet/serverless-mode.md). For recovering a parked partition or a paused observer, see [Automatic recovery from quarantine](./index.mdx#automatic-recovery-from-quarantine).
