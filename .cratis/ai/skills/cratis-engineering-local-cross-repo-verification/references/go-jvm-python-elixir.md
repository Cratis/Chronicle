<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/go-jvm-python-elixir.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Other language boundaries

These are manual equivalents, not additional helper subcommands. Discover the
producer/consumer build conventions first. Keep local configuration outside
tracked files, preserve edits, run consumer regression specs, remove substitutions
and inspect the diff before commit/push. The helper's leak check does not recognize
all Go, Gradle, Python or Mix substitutions; manual inspection is required too.

## Go modules

Prefer an untracked workspace for a source-level reproduction:

```bash
# In an ignored scratch directory, using absolute producer/consumer module paths:
go work init /absolute/consumer/module /absolute/producer/module
export GOWORK="$PWD/go.work"
cd /absolute/consumer/module
go test ./...
```

The producer's module path must match the dependency; add the actual module roots
in a multi-module repository. Alternatively use `go mod edit
-replace=example.org/producer=/absolute/producer/module` in a disposable consumer
copy. `replace` changes go.mod, so never commit it; preserve the original file and
remove only that override afterward. Workspace/replace testing does not prove a
published module archive's contents. If packaging is part of the bug, also inspect
and test the release module layout. Unset `GOWORK` and inspect go.mod/go.sum and
workspace files after proof; a scratch workspace must not be committed.

## Kotlin / JVM

For compatible Gradle builds, use composite substitution:

```bash
cd /absolute/consumer
./gradlew --include-build /absolute/producer test
```

Gradle substitutes matching project coordinates; inspect `dependencies` or
`dependencyInsight` to confirm the intended module, rather than trusting a green
task that still used a released artifact. Where composites do not match the
published layout, publish the producer's configured publications with
`./gradlew publishToMavenLocal` using `$stamp` as the configured publication
version, then select that version through an untracked consumer init
script/repository override. Configure
`mavenLocal()` only in that override and confirm effective coordinates. Do not
assume every build accepts a generic version property. Remove only that local
version's artifacts after review, not the whole Maven cache. Remove overrides and
inspect settings/build scripts and lockfiles before commit/push.

## Python

Prefer the actual wheel for package-boundary proof, in a disposable virtualenv:

```bash
# Requires the repository's existing build frontend; do not install tools blindly.
python -m build --wheel --outdir /absolute/scratch/wheels /absolute/producer
python -m venv /absolute/scratch/venv
/absolute/scratch/venv/bin/python -m pip install /absolute/scratch/wheels/<wheel>.whl
# Install the consumer's remaining dependencies and execute its regression tests
# with that interpreter, confirming it did not replace the selected wheel.
```

If the consumer needs an editable install, use that environment's `pip install -e
/absolute/producer` and state that this proves source integration, not wheel
contents. Inspect installed versions and module paths; reusing a global Python
can silently exercise another copy. Never record absolute wheel/editable paths in
shipped requirements or lockfiles. Remove the task-owned virtualenv/wheels after
review and inspect the consumer diff.

## Elixir

In an isolated consumer copy, temporarily replace the relevant Mix dependency
with its exact project name and `path: "/absolute/producer"`; use `override: true`
only where transitive resolution requires that substitution. Run `mix deps.get`
and the consumer's regression `mix test` tasks. Inspect `mix deps` for the selected
path. This tests local source, not the contents of a Hex release. Restore only the
local dependency edit and corresponding lockfile change; re-fetch released deps
before final gates. Inspect mix.exs/mix.lock and remove task-owned build/cache
outputs without deleting unrelated work.
