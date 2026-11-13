---
title: Check a release against a kernel
description: Find out before you deploy whether a client release is compatible with the Cratis Chronicle kernel it will connect to, including whether its event type schemas would be accepted.
---

A client release can be built against a newer Chronicle than the kernel it is deployed to. Registration then fails at startup, after the deployment, with an error about an event type schema. Checking the release against the production kernel first moves that failure to before the rollout.

The check reads from the kernel and writes nothing: no event type is registered, no event is appended and no cache is evicted. It applies the same rules registration applies, so a release the check accepts is a release the kernel accepts.

## What is checked

| Check | Question it answers |
|---|---|
| `chronicle-kernel` | Is the kernel at least the version the release needs? |
| `chronicle-contracts` | Does the kernel serve every operation the release's wire contracts use? |
| `event-schemas` | Would the kernel accept every event type registration in the release? |

The overall verdict is `compatible` only when every check is. A check that could not be answered, such as an unreachable kernel or an older kernel without the schema check, is `unknown`, which is treated as not compatible so that a release is never approved on missing information.

## Describe the release

The release is a JSON file in the `cratis.release/v1` format, which any client can produce:

```json
{
  "clientType": ".NET",
  "clientVersion": "19.157.0",
  "protocolVersion": "1",
  "minimumKernelVersion": "19.156.1",
  "contractsDescriptorSetPath": "contracts.desc",
  "eventTypes": [
    {
      "id": "UserRegistered",
      "generation": 1,
      "schema": "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}",
      "generations": [ { "generation": 1, "schema": "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}" } ],
      "migrations": []
    }
  ]
}
```

The event types are the registrations the client sends when it connects, and the schemas are the JSON schemas it generates for them.

## Run the check

Pass the file to the Chronicle command line tool, against the kernel you deploy to:

```shell
cratis chronicle check-release --release release.json -o json
```

The JSON output lists one entry per finding:

```json
{
  "format": "cratis.compat/v1",
  "release": "v19.157.0",
  "verdict": "incompatible",
  "checks": [
    {
      "check": "event-schemas",
      "expected": "event type UserRegistered generation 1 accepted",
      "actual": "schema differs at $/properties/name: type",
      "verdict": "incompatible",
      "action": "keep the stored schema for generation 1 or add a new generation"
    }
  ]
}
```

Every entry carries the same five fields: `check`, `expected`, `actual`, `verdict` and `action`. For a rejected event type, `actual` names the generation and the first schema keyword that differs, with its location in the schema.

Schema differences that registration tolerates, such as `title`, `description`, `examples`, `deprecated`, `$comment` and `contentEncoding`, are not reported.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Every check is compatible. |
| `5` | At least one check is incompatible. This wins over an unknown result. |
| `2` | The kernel could not be reached. |
| `4` | Authentication to the kernel failed. |

## Run it in a pipeline

Run the check against the production kernel before the deployment step, and let the exit code stop the pipeline:

```yaml
- name: Check the release against production
  run: cratis chronicle check-release --release release.json --server "$CHRONICLE_PRODUCTION" -o json
```

A non-zero exit fails the job, so an incompatible or unverifiable release never reaches the deployment step.
