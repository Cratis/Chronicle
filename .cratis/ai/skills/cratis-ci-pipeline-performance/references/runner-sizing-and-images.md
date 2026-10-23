<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/runner-sizing-and-images.md -->
# Runner sizing, images and cache layers

Use this when every job is uniformly slow, when jobs spend time pulling or
installing tools, or when deciding how to warm workspaces.

## Contents

- Cores per job before job count
- Container images
- Tools baked into the runner image
- Cache layers and warm workspaces
- Publish stages

## Cores per job before job count

Undersized runner pods (a small CPU request or limit) make every job slow, and more
jobs then multiply the slowness. Before sharding:

1. Print `nproc` and the memory limit at the start of a slow job, and read the
   runner's resource configuration.
2. If compile or test steps scale with cores, a larger runner or a higher CPU
   request is the first lever. Runner capacity is an organization setting; request
   it from the owner with the measured evidence, do not work around it in YAML.
3. Re-measure with the same `ci-timings.sh` command.

## Container images

- Start only the services a job uses; pull only those images. Service containers
  that a pull-request job never touches are pure wait.
- Pull images in parallel (`docker pull a & docker pull b & wait`, checking each
  exit status) rather than serially.
- Do not pull a large release image to obtain one small tool; install or copy the
  tool directly.
- Avoid re-pulling a large handoff image in every downstream job; see publish
  stages below.

## Tools baked into the runner image

Browsers, system packages and language toolchains installed on every run cost
minutes every run. Bake them into the runner image and keep the install step only as
a version check. Keep the image's contents versioned so a change is reviewable.

## Cache layers and warm workspaces

Prefer content-addressed caches, which are correct by construction, before
warm volumes:

1. A registry mirror or pull-through cache for images.
2. A remote or registry-backed BuildKit cache (see `caching.md`).
3. Package caches keyed by lock files.

Warm workspaces (a prepared checkout and build output on a volume) are only safe
when they come from a snapshot of a known-good state. Round-robin reuse of
whichever runner last ran leaves stale or poisoned output. Publish and release
builds stay cold: a clean build is part of what makes the artifact trustworthy.

## Publish stages

- Run independent stages in parallel, for example build alongside a dependency
  audit, and join before publishing.
- Build several images with `docker buildx bake` and a registry cache instead of
  one serial build per image.
- When one job builds an image that later jobs consume, keep the handoff small or
  rebuild from cache, whichever the slowest-steps table shows is cheaper.
- Keep the publish-path guardrails in `publish-pipelines.md`.
