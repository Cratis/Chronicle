# gRPC Contract Compatibility Validation

This directory contains scripts for validating gRPC API contract compatibility to detect breaking changes.

## Overview

The compatibility validation system helps ensure that changes to the Chronicle gRPC API surface don't introduce breaking changes that would cause wire incompatibility with existing clients.

## Components

### Scripts

#### `generate-grpc-schema.sh`
Generates a Protocol Buffers schema definition from the Chronicle Contracts project.

**Usage:**
```bash
.github/scripts/generate-grpc-schema.sh <output-file>
```

**Example:**
```bash
.github/scripts/generate-grpc-schema.sh schema.proto
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

#### `update-pr-description.sh`
Updates a pull request description with a warning about gRPC breaking changes.

**Usage:**
```bash
GH_TOKEN=<token> .github/scripts/update-pr-description.sh <pr-number> <breaking-changes>
```

**Example:**
```bash
GH_TOKEN=$GITHUB_TOKEN .github/scripts/update-pr-description.sh 123 "Service 'Foo' was removed;Method 'Bar' signature changed"
```

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
   - Adds a warning to the PR description if breaking changes are detected

2. **Publish Workflow** (`publish.yml`):
   - Runs on releases
   - Compares against the `main` branch
   - Ensures release notes capture any breaking changes

## Breaking Changes Detection

The system detects the following types of breaking changes:

1. **Service Removal**: When a gRPC service is removed
2. **Method Removal**: When an RPC method is removed from a service
3. **Signature Changes**: When a method's request or response types change

## Example Output

When breaking changes are detected, the PR description will be updated with:

```markdown
## ⚠️ gRPC API Breaking Changes Detected

This PR introduces **breaking changes** to the gRPC API surface that will cause wire incompatibility with existing clients.

### Breaking Changes:
- Service 'EventStores' was removed
- Method 'Namespaces.GetNamespaces' signature changed

Please ensure:
1. This is intentional and documented in the release notes
2. Version number is bumped appropriately (major version for breaking changes)
3. Migration guide is provided if necessary
```

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
