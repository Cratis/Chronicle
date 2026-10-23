<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/dotnet-nuget.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# .NET and NuGet

## CPM overlay

Execute the helper from the entry point. `pack-nuget PRODUCER STAMP TARGET...`
passes `PackageVersion` and `Version` to Release `dotnet pack`. Use the entire
changed dependency closure, not just the top-level package. Unchanged dependencies
may remain released; inspect the resulting nuspec and restore graph to distinguish
them from an accidental published fallback.

`use-nuget CONSUMER STAMP [ID...]` requires a root `Directory.Packages.props`.
Without IDs it selects every package with `<id>.<stamp>.nupkg` in the feed,
including transitive dependencies absent from the consumer props. The generated
overlay imports the consumer's real props, uses `PackageVersion Update` for IDs
already pinned there and `PackageVersion Include` for the others, and enables
`CentralPackageTransitivePinningEnabled` so the added IDs pin transitive packages.
An `Update` alone cannot add an absent ID. It does not copy or edit the props in git.

```bash
bash "$local" use-nuget "$consumer" "$stamp" Cratis.Chronicle Cratis.Chronicle.CodeAnalysis
# Source the absolute env path printed by the command.
source /absolute/session/path/env
cd "$consumer"
dotnet restore path/to/consumer.slnx
dotnet build path/to/consumer.slnx -c Release --no-restore -warnaserror
```

The printed properties are `DirectoryPackagesPropsPath` and
`RestoreAdditionalProjectSources`. For an IDE, launch it from that shell, or pass
the printed `-p:` properties explicitly to its build. Verify restore uses the
stamp in `obj/project.assets.json`; shell environment changes do not reconfigure
an already-running IDE. A config overlay printed below must also reach IDE restore.

The `CRATIS_LOCAL_RUN` adapter applies to pack steps, not these restore/build
commands; route each heavy command separately through the host's build queue.

## Source mapping

If a tracked root NuGet config has `packageSourceMapping`, the helper prints an
additional restore command with `--configfile <session>/nuget.config`. Use it.
The generated config preserves existing entries, makes relative sources absolute,
adds a `cratis-local` source, removes exact selected-ID mappings from remote
sources, and maps those IDs exactly to the local source. Exact local matches take
precedence over wildcard patterns. Inspect the generated config and restore output
for nested/imported/unsupported configuration; do not assume this is a general
NuGet XML rewriter. An incomplete mapping or existing `cratis-local` key is refused.

## Versions and cache

Generate a fresh stamp with the helper, not `0.0.0-local`. The next-patch prerelease
must sort above the versions required transitively or NuGet may report NU1605.
A stale highest tag or dependency with a newer minimum needs investigation, not a
warning suppression. The suffix is `-local.<12-digit-UTC-minute>.g<sha7>`; the `g` prefix makes an
all-digit SHA unambiguously nonnumeric in SemVer. Dirty edits are not encoded.
Wait for a distinct UTC minute or commit before repacking changed contents.

The existing global cache is shared safely only because local versions have
unique directories. Do not repack changed bits under the same version. Use
`clean --stamp "$stamp"` to list that stamp's version folders, then `--apply`
after review; do not delete the entire NuGet cache.

## Non-CPM escape

The helper refuses a consumer without root CPM props. Use an untracked isolated
project/worktree copy with explicit `PackageReference` versions and restore with
`--source "$CRATIS_LOCAL_FEED/nuget"` plus the normal remote sources. Supply every
needed source because `--source` replaces configured sources. Inspect the resolved
graph. Never edit versions intended to ship merely to make local proof work.

## Restoration and drift

`restore` removes session props, generated config and env files, then runs `check`.
It cannot unset variables in the calling shell:

```bash
bash "$local" restore "$consumer"
unset DirectoryPackagesPropsPath RestoreAdditionalProjectSources
bash "$local" check "$consumer"
```

Inspect producer and consumer diffs. Workbench builds in particular can regenerate
tracked proxies. Prefer a throwaway worktree; revert only known disposable output
with `git checkout -- <generated-path>` after checking it was clean beforehand.
No blanket checkout/reset is part of this workflow.
