---
profile: framework
applyTo: "**/*.go,go.mod,go.sum,**/go.mod,**/go.sum"
paths:
  - "**/*.go"
  - "go.mod"
  - "go.sum"
  - "**/go.mod"
  - "**/go.sum"
---
<!-- cratis-ai-managed: rules/go-engineering.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Go framework parity and module releases

`cratis/engineering/go` composes the Go language profile. Repository-owned
context, not this rule, defines module paths, layout, quality-gate commands,
release configuration and the exact C# parity target.

## Preserve the product contract

Before changing a Go port, inspect its owning C# API, implementation and tests
at the parity target revision. Translate concepts idiomatically, not literally:
context plus errors, constructors/options, explicit registration, named values
and deliberate stream lifetimes. Do not change sibling implementations to
accommodate the port.

Preserve routes, verbs, camelCase JSON where contracted, envelopes, UUID byte
order/encoding, protobuf fields and RPCs, tenancy, event ordering, retry and
checkpoint semantics. Record each deliberate divergence in repository-owned
parity documentation with source path/revision, Go surface, status, executable
evidence and caller migration implications. A compilation check is not wire,
streaming or lifecycle parity. Unsupported behavior fails explicitly, never as
no-op success. Keep a limited conformance result limited; do not claim whole-product parity.

## Release compatibility

- Review signatures, interface growth, public struct comparability, wrapped
  errors, defaults and wire behavior. API-diff tools supplement consumer tests.
- Experimental v0 breaking APIs require a **minor** bump and migration notes,
  not a patch. From v1, incompatible APIs need a planned major version.
- Apply exactly one release-intent label under the repository's PR policy.
  Check how its configured `Cratis/release-action` maps v0 changes before release.
- Root modules use immutable `vX.Y.Z` tags created through the configured
  release-action workflow. Do not create duplicate manual releases or move tags.
  Nested modules need their own explicitly reviewed tag policy.
- Plan `/v2` migrations across the module directive, imports, examples, consumers
  and parallel support. Neither v0 nor v1 gets a `/v0` or `/v1` suffix.
- Correct a bad release with a new release and, when appropriate, documented
  retractions in a newer published version. Retraction does not erase downloads
  or prohibit an explicit request for the old version.
- Verify release consumption without local workspace/replacement assistance.
  Source/tag availability, module download, consumer compilation and proxy/pkg.go.dev
  indexing are separate results. Indexing delay never justifies retagging.

Adapted from the repository-owned
[Chronicle.Go parity rule](https://github.com/Cratis/Chronicle.Go/blob/v0.9.1/.cratis/ai/rules/go-cratis-parity.md)
and [repository-owned module release guidance](https://github.com/Cratis/Chronicle.Go/tree/v0.9.1/.cratis/ai/skills),
with repository-specific paths and commands removed.
