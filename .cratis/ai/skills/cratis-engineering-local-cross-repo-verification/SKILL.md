---
name: cratis-engineering-local-cross-repo-verification
description: Proves lower-layer Cratis fixes in downstream repositories using local packages and images. Use when a lower-layer fix must be proven in a consumer, before the producer PR for a consumer bug, before requesting or waiting on a release, or when reproducing a consumer bug against lower-layer main. Not for single-repo changes, released-version pin bumps, CI changes, or shipping; use the ship-changes prompt for shipping and cratis-release-notes for release text.
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
Also use this loop to compare the consumer against lower-layer main.

**Local verification replaces waiting, not shipping.** After local proof, release
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
errors. Requires bash and git; add dotnet for NuGet, node and Yarn for npm, Docker
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
- [ ] Generate one stamp; build and pack the producer's full dependency closure.
- [ ] If needed, publish the kernel and assets, then build a matching local image.
- [ ] Activate the consumer overlay and select the local image.
- [ ] Build the consumer, run its regression specs/app action and required gates.
- [ ] On failure, diagnose, fix the producer, create a distinct stamp and repeat.
- [ ] Run `check` before every consumer commit and push, and on the producer.
- [ ] Restore overlays, unset environment overrides, and run `check` again.
- [ ] Review stamp-scoped `clean` dry-run targets, then apply cleanup.

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
separately. A successful pack alone does not prove dependency closure.

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
artifacts. Follow any printed `--configfile` instruction. Run Yarn install after
`use-npm`; it adds file-tarball resolutions, not links to source folders.

### 5. Build and exercise the consumer

From the consumer root, with the printed NuGet environment sourced:

```bash
dotnet build path/to/consumer.slnx -c Release -warnaserror
dotnet test path/to/Integration.csproj -c Release --no-build --filter '<regression>'
```

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

```bash
bash "$local" clean "$consumer" --stamp "$stamp"
# Review listed subjects and stop task-owned containers first.
bash "$local" clean "$consumer" --stamp "$stamp" --apply
```

Pass: expected subjects listed, then removed without force. Cleanup covers local
feed packages, NuGet version folders, discoverable Yarn cache entries, local image
tags and registered sessions; unresolved npm backups block it. Missing Docker/Yarn
probes are reported as skipped cleanup, not proof those stores are empty. Default
fuse: 500 subjects; investigate an unexpectedly large list instead of raising it.
Prefer `--stamp` to `--all` on shared machines.

## Configuration and results

The trusted shell config `${XDG_CONFIG_HOME:-$HOME/.config}/cratis/local-packages.env`
is sourced before defaults. Do not put credentials in it. Environment settings:

| Setting | Default / purpose |
| --- | --- |
| `CRATIS_LOCAL_FEED` | `${XDG_CACHE_HOME:-$HOME/.cache}/cratis/local-feed`; outside tracked files |
| `CRATIS_LOCAL_SESSION_DIR` | `<absolute-git-dir>/cratis-local`; per-worktree overlays/backups |
| `NUGET_PACKAGES` | `$HOME/.nuget/packages`; shared cache, unique version folders |
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
`0`. npm overlay/restore tests run when node is on the inherited PATH; real packing
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
- Read [references/worked-examples.md](references/worked-examples.md) when following Chronicle → Studio, Fundamentals → Arc or Components → Studio.
- Read [references/other-environments.md](references/other-environments.md) when running on another developer machine or an agent host.

Read `scripts/npm-restore-check.cjs` when inspecting the read-only npm restoration
guard; `restore` executes it when npm files differ from the saved overlay state.
