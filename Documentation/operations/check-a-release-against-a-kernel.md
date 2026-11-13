---
title: Check a release against a kernel
description: Find out before you deploy whether the event type registrations of a client release would be accepted by the Cratis Chronicle kernel it will connect to.
---

A client release can be built against a newer Chronicle than the kernel it is deployed to. Registration then fails at startup, after the deployment, with an error about an event type schema. Asking the production kernel whether it would accept the release's event types moves that failure to before the rollout.

The kernel exposes this as the `CheckEventTypeRegistrations` command on the event types service. It reads from the kernel and writes nothing: no event type is registered, no system event is appended and no cache is evicted. It applies the same rules registration applies, against what the event store holds, so a registration the check accepts is a registration the kernel accepts.

:::note
A `cratis` command line command that runs this check from a release file, and a pipeline-friendly exit code, is forthcoming. It is tracked in [Cratis/cli#602](https://github.com/Cratis/cli/issues/602). Until it ships, call the command from your own client or tooling.
:::

## What the command checks

The command takes an event store name and the same event type registrations a client sends when it connects: the event type, its generation, its JSON schema, its generations and its migrations. For each registration it checks that:

- the migration chain from the first generation to the current one is unbroken,
- no schema differs from the schema stored for a generation that already exists,
- the compliance and protection metadata of an existing generation has not been removed or weakened, and
- every property a migration refers to exists in the schema it applies to.

A registration whose schema or migration cannot be read is rejected with the reason, and does not stop the other registrations from being checked.

Schema differences that registration tolerates, such as `title`, `description`, `examples`, `deprecated`, `$comment` and `contentEncoding`, are not reported.

## Read the result

The result holds one entry per registration, in the order the registrations were given. Every entry carries the same fields:

| Field | Meaning |
|---|---|
| `eventType` | The identifier of the checked event type. |
| `generation` | The generation the entry is about. For a changed schema this is the generation that differs. |
| `accepted` | `true` when the kernel would accept the registration. |
| `reason` | Why the registration was rejected. Empty when accepted. |
| `keyword` | For a changed schema, the first schema keyword that differs, such as `type`. |
| `path` | For a changed schema, where in the schema it differs, such as `$/properties/name`. |

A rejected property type change looks like this:

```json
{
  "results": [
    {
      "eventType": "UserRegistered",
      "generation": 1,
      "accepted": false,
      "reason": "Event type 'UserRegistered' at generation 1 has a schema that differs from the already registered schema. Schema changes are not allowed without creating a new generation.",
      "keyword": "type",
      "path": "$/properties/name"
    }
  ]
}
```

The release is compatible only when every entry is accepted. An entry that is not accepted means the registration would fail, so treat a failed or unanswered check as not compatible rather than approving a release on missing information.

Keep the stored schema for the generation, or add a new generation with migrations, to resolve a rejection.
