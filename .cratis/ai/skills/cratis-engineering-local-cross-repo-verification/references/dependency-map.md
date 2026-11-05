<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/dependency-map.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Dependency map

`scripts/dependency-map.tsv` is a reference table of the producer-to-consumer edges between
Cratis repositories, the pack targets of each producer and the package IDs each ships.
`cratis-local-packages.sh` reads it (or the file in `CRATIS_LOCAL_MAP`, or `--map FILE`) to derive
one plan: which repositories to pack, in which order, and which IDs the feed must hold. It holds
no versions; read pins live from the consumer manifests (`Directory.Packages.props`,
`package.json`, `Dockerfile`).

## Rows

Tab-separated, `#` starts a comment.

| Row | Fields | Meaning |
| --- | --- | --- |
| `pack` | repo, eco, target, `PROP=VAL,...` | A pack target; the optional MSBuild properties are the `-p:` arguments the producer's own publish workflow passes. `closure` prints each as one `--property NAME=VALUE` argument of that row's `pack-nuget` command |
| `ids` | repo, eco, comma-separated IDs | The packages the producer ships in that ecosystem |
| `uses` | consumer, producer, eco, manifest | An edge; the manifest is the pin file, or the Yarn project directory for npm |
| `image` | repo, dockerfile, tag suffix | A producer image a consumer builds on |

`eco` is `nuget`, `npm` or `image`. For npm, `.` means the repository root.

## Using it

- Find every `uses` row whose producer is the repository you changed; those consumers
  pin its packages. A consumer may also reach the producer through another repository
  (Fundamentals reaches Chronicle through Arc as well as directly).
- Pack upstream first. Overlay the earlier producer into the next repository with
  `use-nuget`, pack that, then `restore` it. Use one stamp for the whole chain: `stamp`
  takes several producers, so generate it once and reuse the string.
- Read the closure: `closure [--train] CONSUMER=PATH PRODUCER=PATH...` (producers in dependency
  order) prints the commands to run, in order, from the plan; `plan CONSUMER PRODUCER...` prints
  the plan rows alone. Both exit 2 and print nothing when the plan is invalid.
- Pack properties are `pack-nuget --property NAME=VALUE` arguments, one per `-p:` of that command
  only; never export them (an exported `IsPackaging=true` reaches Chronicle's nested `dotnet run`
  code generator and fails with a multi-target error).
- `--train` prints the train stamp (see [train-mode.md](train-mode.md)) instead of a local stamp.

## Pack plan and verification scope

The plan is a set of `(producer, ecosystem)` pack rows, overlay rows (a producer and the upstream
producers it consumes) and consume rows (the consumer's own edges), derived from the `uses` rows
for the names you list. It is rejected (exit 2) for an unknown or repeated name, no edge, a wrong
order, or a planned pack without `ids` or `pack` rows.

- Run `verify-pack STAMP --consumer C --producer P...` once, after the last pack. It checks the
  planned IDs exist at exactly STAMP and each planned NuGet dependency is exactly STAMP, [STAMP] or [STAMP, ) (an unreadable dependency element fails).
- After `use-*` and restore, `verify-assets CONSUMER-PATH STAMP --consumer C --producer P...`
  checks the same plan inside the consumer: no planned ID resolved at another version, and no
  producer the consumer uses left unconsumed. `--json` lists the resolved IDs only.
- Each command derives and checks the plan itself and keeps no state between runs.
- `verify-pack STAMP NAME...` and `verify-assets CONSUMER STAMP` without a plan are manual and
  weaker: they check the named repositories' IDs, or every stamped package the consumer resolves.

## Facts about the chains

- Arc and Chronicle depend on each other. Arc's Chronicle-facing packages are built against
  a released Chronicle, and Chronicle's clients against a released Arc. The same holds for
  Chronicle and Orleans. Record the nuspec dependencies and do not break the cycle.
- Image and package pins are coupled. A consumer that builds `FROM` a producer image may
  derive the tag from the producer's NuGet pin; check its publish workflow before tagging a
  local image.
- A repository you did not name keeps its released version: Stage reaches Arc, Chronicle
  reaches Orleans. A binary break through that hop is a finding about the consumer's pins.
- The current Screenplay rows list only npm IDs although Stage and Studio consume its NuGet
  packages; `closure` exits 2 for them until the map has the Screenplay NuGet `ids`. Read the
  IDs from the producer's publish workflow and add them first.

## Extend it

Add `pack` rows only from the producer's publish workflow on the default branch, not a
stale clone. Add `ids` for every package a consumer must resolve. Add one `uses` row per
consumer manifest that pins the producer.
