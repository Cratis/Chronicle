# gRPC Contract Compatibility Validation

This document covers the gRPC contract-compatibility scripts in this directory. The other
scripts here (coverage collection, the integration matrix generator, the test retry runner,
issue analysis) are documented by the header comment in each script.

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

Runs on every pull request that touches `Source/**`, and is also callable as a reusable
workflow. It generates the schema for the PR branch and for the base branch, compares
them, and **fails when breaking changes are detected**.

**Inputs:**
- `base-ref`: Base branch reference to compare against (e.g., `main`, `develop`). Only
  populated for `workflow_call`; on a plain `pull_request` the base branch of the PR is used.

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

## Approving an intentional breaking change

The gRPC wire contract is public API — every deployed client speaks it — so a detected
break fails the check by default. When the break is intentional, add the
**`grpc-breaking-change-approved`** label to the pull request and re-run the job. The
workflow reads the labels straight from the event payload, so no token is involved.

Applying that label is the deliberate act of accepting wire incompatibility, and it
implies the **`major`** semver label: a broken public contract is a major release by
definition. The change also belongs in the PR description so it reaches the release notes.

## Breaking Changes Detection

The system detects the following types of breaking changes:

1. **Service Removal**: When a gRPC service is removed
2. **Method Removal**: When an RPC method is removed from a service
3. **Signature Changes**: When a method's request or response types change

## Example Output

When breaking changes are detected, the job annotates the run and fails:

```text
⚠️  Breaking changes detected:

  - Service 'EventStores' was removed
  - Method 'Namespaces.GetNamespaces' signature changed

Error: Breaking gRPC contract changes detected: Service 'EventStores' was removed;Method 'Namespaces.GetNamespaces' signature changed
Error: If this break is intentional, add the 'grpc-breaking-change-approved' label to the
pull request (and the 'major' semver label), then re-run this job.
```

Both generated schemas and the raw comparison output are uploaded as the `grpc-schemas`
artifact on every run, so a failure can be inspected without re-running anything.

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
