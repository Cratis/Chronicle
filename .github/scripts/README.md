# CI Scripts

## Core spec partitioning

The three ordinary `specs-other` jobs still cover all 25 ordinary projects.
`Core.Specs.csproj` is planned as two units named
`Core.Specs.csproj#core-part-K-of-2`; no workflow, artifact, or required-check name changes.
Other projects retain their existing invocation. An unsharded invocation still runs Core once.

`core-spec-partition.json` contains disjoint namespace/class prefixes and relative weights.
Parts are assigned by longest-processing-time-first (descending weight, ordinal prefix
on ties, lowest numbered part on equal totals). The ordinary planner compares normal LPT
with Core-first placement, keeping the lower predicted critical path. It never forces
Core spreading when stacking is faster.

Train-8 run `43190307308`, attempt 1, measured Core parts at 80.179 / 119.541 / 103.457 /
101.729 seconds including validation. The model subtracts 55 seconds fixed overhead from
each old part, scales its namespace weights to the residual, and conserves 184.906 seconds
of variable work. Every new part pays 55 seconds; mean per-shard inventory overhead is
32.308 seconds, separate from project intervals. The original weights came from run
`43166203967` TRX durations plus 0.003 seconds per test. Train-8 artifacts also contain
per-test TRX durations, but no per-namespace wall intervals.

Two parts predict 147.454 / 147.452 seconds each, with ordinary coverage steps at
287.756 / 286.624 / 286.620 seconds (project-only: 255.448 / 254.316 / 254.312).
Three parts have a perfect-placement lower bound of 305.333 seconds per shard including
inventory overhead; every additional part increases that bound. Thus two minimizes the
predicted slowest shard among counts of two or more. These are model predictions, not
observed savings: train 8 actually took 263 / 388 / 320 seconds; train 7 took 393 / 254 / 224.

For a wall-time refresh, preserve the partition used by the measured run, obtain its jobs
JSON and all three ordinary job logs, then run (logs in shard order):

```bash
node .github/scripts/refresh-spec-shard-timings.mjs 43190307308 previous-partition.json jobs.json shard-1.log shard-2.log shard-3.log
```

The refresh rejects incomplete/duplicate measurements, compares part counts with explicit
fixed overhead, and checks the exact-once partition before writing either timing table.
It stops if `CORE_PARTS` differs from the model-selected count; update that constant and
its count-specific regression expectations before refreshing. Always calibrate from the
partition that produced the logs, not an already recalibrated table.

For source additions, refresh prefixes with a complete, non-overlapping set of Core TRX
results (one whole run, or every part of a run), not duplicate downloads. This replaces
wall calibration with TRX weights, so recalibrate against a matching measured run afterward:

```bash
node .github/scripts/core-spec-partition.mjs --refresh path/to/Core/results.trx
node .github/scripts/core-spec-partition.mjs --check
node .github/scripts/spec-shards.mjs --dry-run
node --test .github/scripts/test-spec-shards.mjs
```

The refresh groups namespaces by area, recursively splitting groups above one eighth of
measured weight down to `for_*` boundaries. Direct spec classes receive class-specific terms
when their child namespaces are split. Units without measurements receive a small positive
floor. The checked-in weights sum to one; each part must be nonempty.

The guard covers every source class, record, struct, interface and enum, including abstract
and nested types, regardless of detected tests or inheritance. Nested FQNs use `+`, and generic
definitions include their arity. Only the five named, test-free helpers in
`core-spec-exclusions.json` are exempt. Exclusions must exist and remain test-free; test
attributes, `should_`/`test_` methods, and Specification/Scenario bases on a helper, source
base or any by-name derivative reject the exclusion. This conservative closure also follows aliases and
generic/nested base names without relying on exact inheritance resolution.

The guard evaluates actual substring matching against every non-excluded type FQN and rejects
zero or multiple matching parts, including middle-of-name collisions. Trailing dots prevent
sibling-name collisions. New types outside the recorded prefixes fail closed with the refresh
command. Unsupported declaration syntax fails closed rather than silently omitting a type.
Spec-class discovery is only for reporting; weighting sums TRX durations over guarded types.
The guard is imported by the existing `test-spec-shards.mjs` CI gate, with synthetic uncovered,
double-covered, nested/generic inheritance and unsafe/stale exclusion regression cases.

Each part uses the same built executable and the existing ordinary/runsettings exclusions,
ANDed with its OR-filter. Its TRX and Cobertura files are written under a distinct
`-core-part-K` directory; unit identities also remain distinct in completion receipts.
The existing path-based coverage merge retains every part's report and unions production hits.
Train 8 confirmed the four-part test total of 27,040. All four downloaded Core TRX files
also match the new two-part filters exactly once. CI must still measure the new two-part
wall times and confirm coverage union before treating these predictions as achieved savings.

## gRPC Contract Compatibility Validation

This directory contains scripts for validating gRPC API contract compatibility to detect breaking changes.

## Overview

The compatibility validation system helps ensure that changes to the Chronicle gRPC API surface don't introduce breaking changes that would cause wire incompatibility with existing clients.

## Components

### Scripts

#### `generate-grpc-schema.sh`

Generates a Protocol Buffers schema definition from the Chronicle Contracts project.

**Usage:**

```bash
.github/scripts/generate-grpc-schema.sh <output-file> [repo-root]
```

**Parameters:**
- `output-file`: Path where the generated schema will be saved
- `repo-root` (optional): Root directory of the repository. If not provided, auto-detects based on script location.

**Example:**

```bash
.github/scripts/generate-grpc-schema.sh schema.proto
# Or with custom repo root
.github/scripts/generate-grpc-schema.sh schema.proto /path/to/repo
```

#### `compare-grpc-schemas.sh`

Compares two gRPC schema files and detects breaking changes.

**Usage:**

```bash
.github/scripts/compare-grpc-schemas.sh <baseline-schema> <current-schema>
```

**Example:**

```bash
.github/scripts/compare-grpc-schemas.sh baseline.proto current.proto
```

Exit codes:
- `0`: No breaking changes detected
- `1`: Breaking changes detected

### Workflows

#### `grpc-compatibility.yml`

Reusable workflow that checks for gRPC contract compatibility.

**Inputs:**
- `base-ref`: Base branch reference to compare against (e.g., `main`, `develop`)

**Outputs:**
- `has-breaking-changes`: Boolean indicating if breaking changes were detected
- `breaking-changes`: Semicolon-separated list of breaking changes

**Usage in other workflows:**

```yaml
jobs:
  check-grpc:
    uses: ./.github/workflows/grpc-compatibility.yml
    with:
      base-ref: ${{ github.event.pull_request.base.ref }}
```

## Integration

The gRPC compatibility check is integrated into:

1. **Pull Request Workflow** (`pull-requests.yml`):
   - Runs on every PR
   - Compares PR branch against the base branch
   - Surfaces any breaking changes through the workflow's `has-breaking-changes` and `breaking-changes` outputs

2. **Publish Workflow** (`publish.yml`):
   - Does not run this check or the wire and grain compatibility gates. Publishing only publishes: the pull request that is being merged has already passed them, so repeating them only added their runtime to every release.
   - The wire contract is checked against every released minor of the current major by `wire-compatibility.yml`, called from the pull request build (`dotnet-build.yml`).

## Breaking Changes Detection

The system detects the following types of breaking changes:

1. **Service Removal**: When a gRPC service is removed
2. **Method Removal**: When an RPC method is removed from a service
3. **Signature Changes**: When a method's request or response types change

## Development

To test the scripts locally:

```bash
# Generate schema from current code
.github/scripts/generate-grpc-schema.sh current.proto

# Generate schema from main branch
git checkout main
.github/scripts/generate-grpc-schema.sh baseline.proto
git checkout -

# Compare them
.github/scripts/compare-grpc-schemas.sh baseline.proto current.proto
```
