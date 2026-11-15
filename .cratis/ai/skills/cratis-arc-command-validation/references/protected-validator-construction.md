<!-- cratis-ai-managed: skills/cratis-arc-command-validation/references/protected-validator-construction.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Protected validator construction and key metadata

## Plain read models under On

Requires Arc 22.106.0 for plain Chronicle read-model dependencies in
`CommandValidator<T>` constructors under read protection `On`. Those reads share
the command's fold cache and append guard with `Provide`/`Handle`. Let convention
discovery register a transient validator. Scoped/singleton/factory/instance
registrations are refused; a plain transient self-binding is also allowed.
Ineligible construction fails closed; never fall back to unguarded reads. Use
`[LookUpBy(nameof(Command.Property))]` on each constructor read for multiple
keys. Another source/stream can instead be read explicitly in `Provide`.

## Explicit decision tokens outside On

Requires Arc 22.126.0 for the opt-in construction of validators taking
`DecisionRead<T>` or command-aware `IDecisionReads` on `[ProtectedDecision]`
commands outside `On`. This is **untrimmed JIT only**, not trimmed JIT or
NativeAOT, and not a switch making every DI container trusted.

Install `ProtectedDecisionServiceProviderFactory` from
`Cratis.Arc.DependencyInjection` as the host's actual Microsoft DI factory
**after** `AddCratisArc`, passing the desired `ServiceProviderOptions` explicitly.
Without them both `ValidateScopes` and `ValidateOnBuild` default to false; host
Development settings are not inherited. `BuildProtectedDecisionServiceProvider`
is for independently owned service collections, not a second root inside the
running host. Execute in a native child scope (or the pipeline's scope-owning
overload), never directly on that root. Default `CommandScenario` does not opt in.

Register Chronicle through Arc's `WithChronicle`. Do not replace readers or
re-register tokens. Arc records origins at root construction; copied descriptors,
a wrapper provider, custom container or later collection mutation cannot acquire
that evidence.

Every validator/collaborator needs exactly one public constructor and a fully
inspectable transient graph. Arc constructs fresh instances for the invocation.
It refuses scoped/singleton validators or collaborators, application validator
bindings, factory/instance dependencies, cycles, keyed/open generic/deferred or
optional dependencies, provider/scope accessors and collections (including token
collections). Plain models and raw `IReadModels`, `IEventStore` or `IEventLog`
are refused on this path: use tokens or choose `On`.

Requires Arc 22.126.0 for
`services.AddProtectedDecisionValidator<TValidator>(configure)`. Use it instead
of an application DI factory when adding rules. The callback runs once on the
actual fresh instance before validation; thrown callbacks fail the command and
callback denials are not bypassed. It can replace one Arc convention binding,
not an explicit application registration. Extra explicit bindings refuse the
protected path. Ordinary commands still use normal constructor resolution.

Arc checks rule/component identities before invocation and refuses changed or
cross-scope/reused instances. Construction and cleanup failures block before
commit even with allowed-severity overrides. Arc disposes owned validators and
collaborators in reverse order; DI owns read leaves. This is not a sandbox for
trusted constructor/callback code, mutable captures, static reads or nested-child
rule changes. `Validate` stays advisory and creates a separate invocation; only
Execute enrolls its decision for append-time checking. Under `Off`, choose the
blocking severity needed for business denials.

## Generated key metadata is not validator certification

`CommandReadModelKeys` registers command property/model-key metadata.
Requires Arc 22.125.0 for generated composite-key descriptors and
`ProtectedReadModelKeyAccessors`, which registers exact-type composite members.
These optimize matching; they do not authorize a read or certify a validator's
dependency graph.

Known concrete composite keys receive generated complete descriptors. For an
external/runtime key, register `ProtectedReadModelKeyAccessors.Register<T>` with
every public readable non-indexed instance property, including inherited ones,
before commands run. Generated metadata uses `DeclareKeyTypes` for startup
coverage checks; that is not a replacement for registering member accessors.
Duplicate registration fails. Do not omit a member or treat a
base descriptor as covering a derived runtime type. Missing declared metadata
fails with `ProtectedReadModelKeyAccessorMissing`.

Requires Arc 22.134.0 for `ReadProtection:RequireGeneratedMetadata` (false by
default). Enabling it rejects missing command/model metadata instead of using
reflection. `ProtectedReadModelKeyOptions.Strict` is also false by default;
strict composite matching or a restricted runtime requires exact accessors.
Untrimmed JIT can use the compatibility reflection path when strict mode is off.
ARC0030 is the composite coverage warning. Do not suppress it or promise
NativeAOT support for the unrelated dynamic validator opt-in.

## Specify the boundary

In application specs, call the validator directly with supplied read-model state
and assert its business decision. Keep the one cheap assembly-level application-shape
admission spec in [specs.scenarios.csharp.md](../../../rules/specs.scenarios.csharp.md).

Arc/Chronicle specs own the construction/guard matrix: fresh instances, callback
failures, refused DI graphs, cleanup, key matching and repeated execution. Do not
re-prove that matrix per application validator. A focused application pipeline
spec may prove its own registration/wiring; advisory Validate success alone is
not proof that Execute commits.
