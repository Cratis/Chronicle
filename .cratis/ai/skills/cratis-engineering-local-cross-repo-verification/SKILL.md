---
name: cratis-engineering-local-cross-repo-verification
description: Use when a lower-layer Cratis fix needs proof in a downstream consumer before its producer PR or release. Not for single-repository checks, released-version pin bumps or shipping.
license: LICENSE
compatibility: Bash and git on macOS or Linux; Windows through WSL. Requires the ecosystem tools used by the selected recipe.
---
<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Local cross-repository verification

## Decide before waiting

Use real locally built packages to prove a producer fix at its consumer's package
boundary. A **producer** builds the changed library or image; a **consumer** is the
downstream repository that reproduces the bug. Start before opening the producer
PR whose purpose is that consumer bug, rather than publishing just to find out.
Also use this loop for:

- Consumer-blocking fixes, dependency features, and breaking-change trials.
- Release candidates in their main consumer (Specifications in Fundamentals).
- Cross-repository bisects and build/test timing comparisons against lower-layer main.
- Chained upgrades (Specifications → Fundamentals → Arc/Chronicle) without releasing each hop.
- Release blockers: trying Specifications in Fundamentals can expose a generator bug before publishing.

Prefer a release/preview for trivial changes, external consumers unable to use
the local feed, or CI-only behavior this loop cannot reproduce.

**Local verification replaces waiting, not shipping.** Other waits a candidate
can avoid are in [cratis-engineering-release-trains](../cratis-engineering-release-trains/SKILL.md).
After local proof, release
the producer through the normal authorized workflow and bump the consumer to the
released version. Local feeds, pins and images never go into production or a PR.
Single-repository work needs its own regression specs, not this overlay. Released
pin updates use ordinary dependency maintenance; CI changes use the
[github-actions rule](../../rules/github-actions.md). Shipping uses the
[ship-changes prompt](../../prompts/ship-changes.prompt.md); release text uses
[cratis-release-notes](../cratis-release-notes/SKILL.md).

## Prepare

Execute [scripts/cratis-local-packages.sh](scripts/cratis-local-packages.sh), not
an improvised replacement. Run `bash "$local" <command> --help` for inputs and
errors. Requires bash, git and node for package registry operations; add dotnet for NuGet, Yarn and npm for npm packages, Docker
for images, or the selected language toolchain. Check availability first:

```bash
local=/absolute/path/to/skill/scripts/cratis-local-packages.sh
producer=/absolute/path/to/producer
consumer=/absolute/path/to/consumer
bash --version
git --version
command -v dotnet # substitute node/yarn/docker for the selected recipe
```

The helper works with macOS bash 3.2 and Linux; Windows requires WSL. Match kernel
publish RID and image architecture to the consumer daemon: `linux-arm64` and
`arm64`, or `linux-x64` and `amd64`. Do not claim performance equivalence from a
Debug build or across architectures.

Check disk space and preserve existing edits in both repositories. Use an isolated
worktree when a build regenerates tracked output. Inspect the consumer's own CI
commands before choosing the build, regression spec and full gate commands.

## Copy this checklist

- [ ] Reproduce the consumer bug and select its regression spec/app action.
- [ ] Read [references/dependency-map.md](references/dependency-map.md) for the chain between the named repositories, generate one stamp, then pack each producer in upstream order.
- [ ] If needed, publish the kernel and assets, then build a matching local image.
- [ ] Run `closure` for the consumer and producers, follow the printed commands, and run its one `verify-pack` after the last pack.
- [ ] Set a task ID (`--task ID` or `CRATIS_LOCAL_TASK`, convention `owner/repo#number`), activate the consumer overlay and select the local image.
- [ ] Run `list` (or `list --json`) to inspect recorded producers, tasks and live consumers.
- [ ] Build the consumer, run its regression specs/app action and required gates.
- [ ] On failure, diagnose, fix the producer, create a distinct stamp and repeat.
- [ ] Run `check` before every consumer commit and push, and on the producer; it must report 0 leaks.
- [ ] Upstream first: a downstream ships only after the upstream has released and its pin is bumped to the released version.
- [ ] Restore overlays, unset environment overrides, and run `check` again.
- [ ] Review `finish --task ID`, mark completed work with `--apply`, then review `clean --expired` (or explicit `--stamp`) targets before applying cleanup.

### 1. Stamp

```bash
stamp=$(bash "$local" stamp "$producer")
```

Format: `<next-patch>-local.<12-digit-UTC-minute>.g<sha7>`, based on the highest reachable
`vX.Y.Z` tag. Next-patch prereleases sort above the existing release; arbitrary
`0.0.0-local` versions can violate transitive minimums and cause **NU1605**
downgrades. Confirm tags cover the consumer's released constraints. With no tag,
the helper warns and falls back to a base that must be checked before proceeding.
The `g` prefix keeps even an all-digit SHA nonnumeric in SemVer.
On `release/5.0`, use `stamp --base-version 5.0.0 "$producer"` (also with `--train`);
the strict SemVer core overrides tag discovery verbatim. Ensure it meets consumer minima.
Use the identical stamp for packages and images. A changed build needs a distinct
stamp; do not overwrite a cached version (the minute and commit determine it).

### 2. Build and pack

Run the producer's CI-equivalent build/specs first. NuGet packing builds in Release:

```bash
bash "$local" pack-nuget "$producer" "$stamp" path/to/producer.slnx
```

For npm, complete the repository build before
`bash "$local" pack-npm "$producer" "$stamp"`. Pass: nonzero package count and
exit `0`. Inspect package dependencies: every locally changed dependency ID must
resolve to the stamp, not a published fallback. Chronicle's client solution filter
can omit `Cratis.Chronicle.CodeAnalysis`; pack its code-analysis package project
separately. The package IDs a producer should ship are listed in
[dependency-map.tsv](scripts/dependency-map.tsv); `closure CONSUMER=PATH PRODUCER=PATH...` prints
the stamp, every overlay, one `pack-*` command per map row (with its `--property` arguments) and
ONE `verify-pack` for the whole plan (see [references/dependency-map.md](references/dependency-map.md)).
Run the printed commands in order. A successful pack alone does not prove dependency closure;
`verify-pack` does. After successful packing, the helper atomically records package IDs,
producer worktree, branch and HEAD in `FEED/stamps/STAMP/record.json` (mode `0600`).
Pass `--task ID` to `pack-*` and `use-*`, or set `CRATIS_LOCAL_TASK`; without either,
the recorded task is null. IDs match `^[A-Za-z0-9][A-Za-z0-9._#:/-]{0,127}$` and
are JSON values, never paths. Prefer `owner/repo#number`.

### 3. Build an image when needed

Follow the kernel recipe below before building; it needs published binaries and
Workbench `wwwroot`. From the producer root, for the selected architecture:

```bash
docker build -f Docker/DevelopmentSlim/Dockerfile \
  -t "cratis/chronicle:$stamp-development-slim" .
docker image inspect "cratis/chronicle:$stamp-development-slim"
```

Pass: build exits `0` and inspection shows the expected tag and architecture.
Never push these images. Testcontainers uses a local image when the selected tag
exists in its daemon; ensure the test process uses that same daemon.

### 4. Use in the consumer

```bash
bash "$local" use-nuget "$consumer" "$stamp" Cratis.Chronicle
# Source the absolute session/env path printed by use-nuget.
# For npm instead: bash "$local" use-npm "$consumer" "$stamp"
export CHRONICLE_KERNEL_VERSION="$stamp" # Studio integration specs, if used
```

Pass: the helper reports overridden IDs and restore resolves them to local
artifacts. To isolate NuGet downloads per worktree, set the optional
`NUGET_PACKAGES` export printed by `use-nuget` before restore and keep it set through
`clean`; see [versions and cache](references/dotnet-nuget.md#versions-and-cache).
Follow any printed `--configfile` instruction. Run Yarn install after
`use-npm`; it adds file-tarball resolutions, not links to source folders.

For independent NuGet producer versions, explicitly opt in with
`verify-pack --composition FILE`, `use-nuget CONSUMER --composition FILE
[--pin-transitive]`, then `verify-assets CONSUMER --composition FILE`. See the
[manifest and native-validation workflow](references/dotnet-nuget.md#mixed-version-compositions-opt-in).
No producer is rebuilt; restore before changing a composition or an active legacy overlay.

### 5. Build and exercise the consumer

From the consumer root, with the printed NuGet environment sourced:

```bash
dotnet build path/to/consumer.slnx -c Release -warnaserror
./<integration-project-dir>/bin/Release/<TFM>/<AssemblyName> <runner-specific-filter>
```

Resolve the integration project's actual built spec entrypoint and filter syntax with
[running-specs](../cratis-specifications-csharp/references/running-specs.md); without
apphost use `dotnet <TargetPath.dll>`. Only VSTest uses `dotnet test <project> -c Release
--no-build --filter '<regression>'`. Keep the sourced local NuGet environment.
Substitute the repository's actual commands; for frontend fixes run build, lint,
specs and the previously broken UI action. Pass: the reproducing spec/action now
succeeds, at least one intended filtered spec actually ran, and the consumer's
required CI-equivalent gates pass. Zero matched tests is not proof. Studio local
Debug builds skip analyzers: pass its full-build switch when matching CI. Read the recipe
for that switch. On failure, change the producer and repeat with a distinct stamp;
do not request a release to debug the next round.

### 6. Check before commit or push

```bash
bash "$local" check "$consumer"
bash "$local" check "$producer"
```

Pass: exit `0`, reported file count, `0 leaks`. The scan examines added/changed
tracked lines against HEAD (or `--staged`) plus non-ignored untracked file contents.
It detects stamped local versions, the configured feed path, NuGet overlay/source
properties, package-version updates and local npm file/link/portal references.
It does not inspect committed history, ignored files, active shell variables or
all resolved dependencies. A clean diff can report zero files; it is not proof
that the overlay was inactive. Inspect actual resolution and restore explicitly.

### 7. Restore

```bash
bash "$local" restore "$consumer"
unset DirectoryPackagesPropsPath RestoreAdditionalProjectSources CHRONICLE_KERNEL_VERSION
bash "$local" check "$consumer"
```

Pass: restoration exits `0`, local overlay files are removed and `check` reports
`0 leaks`. npm restore accepts overlay-only resolutions and local tarball lockfile
entries after Yarn install; unrelated edits are refused with backups retained.
Review conflicts and restore only overlay changes manually, then retry. Never
overwrite unrelated work. Reinstall released npm dependencies
and clear image/compose/Gradle selection too. Check the producer for codegen drift;
Workbench builds can regenerate tracked proxies. Prefer a throwaway worktree, or
use `git checkout -- <generated-path>` only after proving those edits are disposable
build output, not pre-existing work.

### 8. Clean

`list [--json] [--stamp STAMP]` is a lock-free, read-only inventory, including legacy
artifacts without records. It shows producer ownership, task state, live/total consumers
and age, plus recorded state. Invalid records appear as `invalid` rows (JSON includes
`error`); listing continues and exits `1`. `use-*` records each consumer, including every
composition member stamp. `restore` restores overlays first, then updates only those
stamps' consumer entries; an invalid selected record warns with its path and exits `1`.
A registry-write failure after activation warns and exits `1`, keeping the overlay for restoration.

```bash
bash "$local" list
bash "$local" finish --task owner/repo#number
bash "$local" finish --task owner/repo#number --apply
```

`finish` is dry-run by default, marks task completion only, and succeeds with a message
if no record names the task and all records are valid. It warns and skips invalid records
without rewriting them, continues with valid matches, and exits `1` if any were skipped.
It never restores consumers or cleans packages automatically.

```bash
bash "$local" clean "$consumer" --stamp "$stamp"
# Review listed subjects and stop task-owned containers first.
bash "$local" clean "$consumer" --stamp "$stamp" --apply
```

`clean --stamp STAMP` validates only that stamp's record; default clean and `--all`
validate all selected records. An invalid selected record refuses cleanup (`2`).

Use `clean --expired` for retention-based cleanup, then add `--apply` after review.
It keeps unrecorded stamps, active compositions and live manifest/overlay references
(in registered sessions or recorded consumer worktrees). Otherwise, a stamp expires
when every non-null producing task is finished (at least one required), all recorded
consumers are gone (at least one required), or a real release at or above its base exists,
in that order. Offline evidence is NuGet `.nupkg.metadata` in the shared or registered
private caches; npm has no offline release check. `--online` opts into NuGet/npm
registry checks with a 10-second timeout; failures provide no evidence. Each stamp
prints `expired STAMP: REASON` or `kept STAMP: REASON`; kept stamps succeed (`0`).
`list --json` reports the same offline decision in `expiry:{expired,reasons,kept}`.
`--expired` cannot combine with `--stamp` or `--all`. It retains the same lock, fuse
and dry-run default. Applying also prunes gone consumers and missing session registrations.

Pass: expected subjects listed, then removed without force. Cleanup covers local
feed packages, NuGet version folders, discoverable Yarn cache entries, local image
tags, stamp record directories and registered sessions; unresolved npm backups block it. Missing Docker/Yarn
probes are reported as skipped cleanup, not proof those stores are empty. Default
fuse: 500 subjects; investigate an unexpectedly large list instead of raising it.
NuGet cleanup scans the active shared cache and every registered private cache.
It removes only recognized local/train version folders. Missing or mismatched
private-cache markers, symlinks and paths outside the consumer's Git directory
refuse the entire request. `restore` keeps private caches. After restoring overlays
and removing live stamps, use `clean "$consumer" --private-cache` to review whole-cache
removal, then add `--apply` to remove the marked cache and its session registration.
Prefer `--stamp` to `--all`. Legacy cleanup refuses the entire request (exit `2`,
also in dry-run) for registered live overlay/manifest references; it reports kept
stamps and paths. Restore/remove references, then retry. This is not an ownership
registry; unregistered consumers need manual review. To retire a hand-rolled
`-local.N` feed, restore released pins/sources and switch to helper stamps; review
old artifacts separately because `clean` does not recognize them. See
[migration](references/dotnet-nuget.md#retire-a-hand-rolled-feed).

## Configuration and results

The trusted shell config `${XDG_CONFIG_HOME:-$HOME/.config}/cratis/local-packages.env`
is sourced before defaults. Do not put credentials in it. Environment settings:

| Setting | Default / purpose |
| --- | --- |
| `CRATIS_LOCAL_FEED` | `${XDG_CACHE_HOME:-$HOME/.cache}/cratis/local-feed`; outside tracked files |
| `CRATIS_LOCAL_SESSION_DIR` | `<absolute-git-dir>/cratis-local`; per-worktree overlays/backups |
| `NUGET_PACKAGES` | `$HOME/.nuget/packages`; active cache scanned by `clean` |
| `CRATIS_LOCAL_TASK` | Optional task ID, convention `owner/repo#number`; `--task ID` on pack/use/finish overrides it |
| `CRATIS_LOCAL_PRIVATE_CACHE` | `0`; `1` opts `use-nuget` into a marked worktree cache, also available with `--private-cache` |
| `CRATIS_LOCAL_CLEAN_MAX` | `500`; cleanup subject fuse |
| `CRATIS_LOCAL_RUN` | Optional executable wrapper receiving `build <label> -- <command...>` |

If the environment has a build queue wrapper, set `CRATIS_LOCAL_RUN` to its absolute
executable path. The helper wraps pack steps only; run other heavy builds, image
builds and container specs through that environment's wrapper separately. No queue
wrapper is required by the corpus. Global `--dry-run` prints mutations while still
doing read-only discovery.

Helper exit codes: `0` success, `1` findings/failed operation, `2` cannot run or
invalid input. Neither `1` nor `2` is a pass. After script changes, execute
`bash "$local" --self-test`; pass requires printed passed/skipped counts and exit
`0`. Registry tests require node; npm overlay/restore tests use the inherited PATH. Real packing
is not exercised by this hermetic self-test. Report any unrun ecosystem checks.

## Why the local loop

A release chain runs producer PR, producer CI and publication, then a consumer
pin bump and consumer CI, with hand-offs between each step. For a Chronicle fix
consumed by Studio that is roughly 15–25 minutes of publishing, a bump pull
request and another 20–30 minutes of consumer CI, and in practice hours of
latency between them. A fix that turns out not to work repeats all of it.

The local loop for the same chain, in run time: packing the client packages
about 1–2 minutes, publishing the kernel a few minutes, building the image
seconds with a warm layer cache, building the consumer's spec project about
6 minutes, and an integration spec class seconds to minutes. Every iteration
stays local. A shared build queue adds its own waiting time on top; that is
machine load, not part of the loop.

The loop also surfaces incompatibilities before anything ships: a consumer that
does not yet supply what a newer kernel requires at startup fails here, in
minutes, instead of after a release and a pin bump.

## Conditional recipes

- Read [references/dotnet-nuget.md](references/dotnet-nuget.md) when using NuGet, CPM, source mapping or IDE restore.
- Read [references/npm-yarn.md](references/npm-yarn.md) when packing frontend libraries or restoring Yarn overlays.
- Read [references/docker-images.md](references/docker-images.md) when publishing a kernel or selecting images in a consumer.
- Read [references/go-jvm-python-elixir.md](references/go-jvm-python-elixir.md) when the boundary uses another language toolchain.
- Read [references/worked-examples.md](references/worked-examples.md) when following Chronicle → Studio, Fundamentals → Arc, Components → Studio, Chronicle → Direct, Screenplay → Stage and Studio, Arc → Chronicle, or Fundamentals → Arc → Chronicle.
- Read [references/train-mode.md](references/train-mode.md) when verifying a downstream repository against a local release train (`stamp --train`).
- Read [references/other-environments.md](references/other-environments.md) when running on another developer machine or an agent host.

Read `scripts/npm-restore-check.cjs` when inspecting the read-only npm restoration
guard; `restore` executes it when npm files differ from the saved overlay state.
