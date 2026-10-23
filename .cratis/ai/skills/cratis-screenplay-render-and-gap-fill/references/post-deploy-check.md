<!-- cratis-ai-managed: skills/cratis-screenplay-render-and-gap-fill/references/post-deploy-check.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Post-deploy check: does the running namespace match the model

After a render is delivered and deployed, `cratis screenplay conform-namespace` compares the
compiled model with what one running Chronicle namespace holds. Render verification and
`conform` never see the running store; this check does. How to read its verdict is in the
toolchain's [observation comparison](../../cratis-screenplay-toolchain/references/observation-comparison.md),
including the CLI version it needs.

## Run it

```bash
cratis screenplay conform-namespace <model-root> --event-store <store> --namespace <namespace> -o json-compact
```

- Pass the model root (the root `.play` file or the folder of one application), never a single
  fragment of a folder.
- Name the event store and namespace explicitly. The built-in default is never assumed, and a
  name differing only in case is reported, not chosen for you.
- `--sequence` selects another sequence (default `event-log`); `--from`, `--to` and `--max-events`
  bound the scan (default `100000` events, up to the tail captured at the start).
- Add `--fail-on-drift` in a pipeline so the exit code carries the verdict.

## Authorization and effect

The command is read-only: it never replays observers, appends events or changes the model, and
it is not a `chronicle` command, so the Chronicle mutation guard does not apply. It still needs a
connection to the target server with permission to read the event type registry, observers and
event metadata. Use the context the user named; resolve it as in `cratis-chronicle-cli-operations`.
Do not log in, switch contexts or point at another environment to make it work. A production
namespace is checked only when the request named it.

## Act on the result

| Exit | Meaning | Act |
| --- | --- | --- |
| `0` | The comparison ran | Read `hasDrift` and the findings; drift alone does not fail without `--fail-on-drift` |
| `7` | Known drift (with `--fail-on-drift`) | Triage each Drift finding: fix the model through the model workflow, or fix the deployment; never edit a rendered file to silence it |
| `8` | Inconclusive (with `--fail-on-drift`), or the scan was interrupted | Report inconclusive; widen coverage or wait for lagging reactors, then rerun once |
| `5` | Model errors, missing store, namespace or sequence, invalid bounds | Fix the input; nothing was compared |
| `2` | Server unreachable | Report the connection failure; do not retry through another route |
| `3` | Chronicle refused or failed a read | Report it; no snapshot was built |
| `4` | Authorization missing for a read | Ask the user for a context with read access; do not widen permissions yourself |

Exit `4` for a refused read and exit `8` for an interrupted scan require Cratis CLI 3.89.1 or later
(`cratis --version`). On an older CLI, treat any exit other than `0` and `7` as "not compared".

A failed read is never an empty store.

## Report

Record the verdict next to the deployment: model identity, event store, namespace, sequence,
examined range against the captured tail, `Complete`, Drift findings and Unknown reasons. A
partial or inconclusive result is reported as such, never as a pass. Remediation that changes
the model or code returns to the render and gap-fill procedure.
