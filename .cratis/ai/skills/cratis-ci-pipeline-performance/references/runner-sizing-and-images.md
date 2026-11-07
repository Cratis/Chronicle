<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/runner-sizing-and-images.md -->
# Runner sizing, images and cache layers

## Contents

- Choosing Cratis runners
- Cores per job before job count
- Container images
- Tools baked into the runner image
- Cache layers and warm workspaces
- Publish stages

Use this when every job is uniformly slow, when jobs spend time pulling or
installing tools, or when choosing runners and deciding how to warm workspaces.

## Choosing Cratis runners

Read the [Infrastructure runner guide](https://github.com/Cratis/Infrastructure/blob/main/Documentation/deployment/github-runners.md)
for the UpCloud scale sets, resource requests and limits, pod ceilings, scratch
storage and autoscaling. Keep those numbers there, not in this skill. The routing
and public-fork safety invariants live in
[github-actions.md](../../../rules/github-actions.md).

Choose by the work, not just the queue length:

| Choice | When it fits | Check before routing |
|---|---|---|
| GitHub-hosted | Public repositories, Windows/macOS jobs | Public hosted execution is free; compare queue and startup time, not just billed minutes |
| `cratis-arc-small` | Lightweight script/API guards such as release-note or work-record checks | No Docker daemon, container actions, service containers or Testcontainers; not for builds, specs, audits or deployments |
| `cratis-arc` | Private Linux builds, specs, dependency audits, deployments and Docker work | Fit peak memory, worker count and scratch use inside the documented limits |

For example, a private-repository release-note script can use
`runs-on: cratis-arc-small`; a private-repository Testcontainers suite needs
`runs-on: cratis-arc`.

- Measure peak memory for the whole job, including Docker services and test
  workers. Leave headroom below the documented limit; an OOM is not evidence that
  more shards will help when those shards share nodes.
- Set build/test worker counts from the pod's CPU and memory limits, not the
  node's advertised cores. A larger in-job worker count can cause contention.
- Compare fan-out with each set's pod ceiling and the shared node pool. More
  shards can wait for slots or node autoscaling instead of shortening the run.
- Include runner startup, image pulls and fresh-node provisioning in wall time.
  For short jobs, merge compatible checks with one setup before adding shards;
  compare the hosted alternative where routing policy permits it.
- Reduce redundant work before requesting capacity: apply PR cancellation,
  change-detected heavy jobs and event deduplication as described in
  `required-checks-and-path-filters.md` and the owning rule.

Do not infer live node counts, runner cost/quotas, network/egress policy or shared
package-cache availability from the YAML. Record missing operational facts in
Infrastructure, not as assumptions in the corpus.

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

Cratis runner pods start with per-job workspaces and Docker storage; neither is
a persistent shared NuGet, npm or BuildKit cache. Restore remote caches explicitly
(see `caching.md`) rather than treating a previous job's downloads as available.
The layers below are options to configure, not claims that Infrastructure provides
them:

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
