<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/caching.md -->
# Caching

## Contents

- Shared Cratis cache actions
- Ephemeral runner pods
- NuGet and Yarn determinism
- Locally packed CI packages
- Docker layer cache
- Scope, eviction and safety

Use this when a job spends measurable time restoring packages, installing a
frontend toolchain or rebuilding Docker layers. Check the slowest-steps table
first: a step under a minute is not worth a cache.

## Shared Cratis cache actions

The `Cratis/Workflows` repository ships two composite actions. Use them instead
of hand-written `actions/cache` steps so every workflow of a repository uses one
key and one path list.

```yaml
- uses: Cratis/Workflows/.github/actions/dotnet-cache@main
- uses: Cratis/Workflows/.github/actions/yarn-cache@main
  with:
    working-directory: Source/JavaScript
```

- `mode` is `auto` by default: pull requests only restore; a push to the default
  branch, a schedule or a manual run on the default branch saves. Use `restore`
  to never save and `save` to always save.
- `dotnet-cache` inputs: `path`, `key-files`, `key-prefix`, `mode`. Keep `path`
  identical everywhere; a different path list is a different cache. Bump the
  version suffix of `key-prefix` to start a fresh cache.
- Outputs: `cache-hit`, `matched-key`, `key`, `mode`.
- A cache is only saved from the default branch, so a repository with no push
  workflow on that branch never warms one. Add a scheduled warm-up workflow that
  runs the same restore and build.

## Ephemeral runner pods

Cratis self-hosted runners use per-job pods: workspace files, package downloads
and Docker data disappear with the job. A warm runner or node image does not make
`~/.nuget/packages`, the Yarn/npm download cache or BuildKit layers persistent.
Use the shared actions above, or explicit `actions/cache` / `setup-*` dependency
caching when the shared actions do not fit. Key downloads on the OS, toolchain and
committed lockfiles (`packages.lock.json`, `yarn.lock` or `package-lock.json`),
and use remote Docker layer caching below. Do not cache `node_modules` as a
substitute for an immutable install.

## NuGet and Yarn determinism

A cache is only as stable as its key. Make the key inputs deterministic.

- NuGet: enable lock files (`<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`)
  and restore with `dotnet restore --locked-mode` in CI, so the lock file hash
  keys the cache and a drifting dependency fails loudly.
- Yarn: commit `yarn.lock` and install with `yarn install --immutable`; the
  install fails instead of rewriting the lock file.
- Cache the package download directories, not `bin`/`obj` output. Build output
  caches across commits are rarely correct; share build output within a run as an
  artifact instead (see `build-once-and-shard.md`).

## Locally packed CI packages

A repository that packs `0.0.0-ci` repeatedly and tests a consumer can restore an
older package with the same ID/version from cached `~/.nuget/packages`. NuGet can
reuse the extracted package without reading the newly packed bytes, so a green
consumer test may exercise the wrong code. A lockfile cache key alone does not
fix an unchanged local package version.

Default: pack a unique prerelease version per run and attempt, and make the
consumer reference that exact version. If the version must stay fixed, set a
per-run packages folder for **every restore/build/test that can restore** in both
the producer and consumer, and do not restore the global package cache into it:

```yaml
env:
  NUGET_PACKAGES: ${{ runner.temp }}/nuget-${{ github.run_id }}-${{ github.run_attempt }}
```

Alternatively, after cache restoration and before consumer restore, remove only
the locally packed package's ID/version directory from the global packages
folder. Keep unrelated dependency downloads cached. A fresh runner is not enough
if the workflow restores stale package bytes from a remote cache.

Verify with `dotnet restore <consumer.csproj> --verbosity normal` followed by the
consumer's focused test command. Pass means restore resolves the newly packed
ID/version from the intended feed/folder and the test exercises behavior added
in that package, not merely that a cached restore succeeds.

## Docker layer cache

```yaml
- uses: docker/setup-buildx-action@v4 # pin by full commit SHA
- uses: docker/build-push-action@v7 # pin by full commit SHA
  with:
    context: .
    push: false
    cache-from: type=gha,scope=api-linux-amd64
    cache-to: type=gha,scope=api-linux-amd64,mode=max
```

- Set a custom `scope` per image and platform. The default scope is shared, so
  two builds overwrite each other's cache and both see misses.
- `mode=max` caches intermediate layers of multi-stage builds; the default caches
  only the final image layers.
- Order the Dockerfile so rarely changing layers (base image, dependency
  restore) come before source copies.
- When an image is built once and pulled by several jobs, pushing it to a
  registry and pulling it costs a transfer per job; measure that pull in the
  slowest-steps table against rebuilding from cache.

## Scope, eviction and safety

- A pull request run can read caches of its own ref and of the base branch; a
  cache saved on the default branch is readable by every branch. A cache saved by
  one pull request is not visible to others.
- Repository cache storage is bounded (10 GB by default). When full, or after a
  week without access, entries are evicted, least recently used first. A large
  rarely used cache can evict a hot one.
- Caches must not contain secrets, tokens or credentials: restored content is
  executed and readable by later runs of the same scope.
- Never restore a cache written by an untrusted pull request into a privileged
  workflow. See `security-and-pinning.md`.
- Verify a cache works by reading the `cache-hit` output (or the restore step
  log) on a second run, then re-measure with `ci-timings.sh`.
