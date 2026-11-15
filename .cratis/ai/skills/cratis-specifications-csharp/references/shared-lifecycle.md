<!-- cratis-ai-managed: skills/cratis-specifications-csharp/references/shared-lifecycle.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Shared specification lifecycle

## Contents

- Eligibility
- Native runner teardown outcomes
- CRSPEC0008
- Keep shared execution on

Requires `Cratis.Specifications.XUnit.v3` or `.XUnit` 5.0. Before 5.0 the shared
lifecycle was an opt-in on xUnit and `CRSPEC0008` was informational; check the package
version before relying on either. NUnit, TUnit and the native runner always share the
instance.

Source: [Running specifications fast](https://github.com/Cratis/Specifications/blob/main/Documentation/running-fast.md)
and [Upgrading to 5.0](https://github.com/Cratis/Specifications/blob/main/Documentation/upgrading-5.0.md).

The shared lifecycle is the house default. Plain xUnit builds a new instance per fact,
so `Establish` and `Because` would run once per fact; the shared lifecycle runs them
once per class, which is what a specification means: one context, one action,
several assertions. Construction, `Establish` and `Because` happen once, all facts share
the instance, and `Destroy` runs once after the last fact, including when
initialization failed. A throwing `Establish` or `Because` fails every fact in that
class.

The package registers the test framework. Delete any hand-written
`[assembly: Xunit.TestFramework("Cratis.Specifications.SpecificationTestFramework", ...)]`;
a duplicate fails the build with `CS0579`.

## Eligibility

A class is shared only when it derives from `Specification`, has a single public
parameterless constructor (or only framework-managed fixture arguments), and has only
plain facts. Theories, `ITestOutputHelper` constructors, custom `IAsyncLifetime`
implementations, custom fact test cases and plain xUnit classes keep the per-fact lifecycle.

Requires Cratis.Specifications 4.4.0 or later for `IDisposable` specifications to share
the lifecycle; earlier versions use per-fact construction. With shared lifecycle enabled,
construction happens once, and `Destroy` runs followed by
one `Dispose` after the last fact, even when `Destroy` fails. Class/collection fixtures remain owned by xUnit,
with assembly fixtures also supported on v3. Use
`[PerFactSpecificationLifecycle]` only on the class whose facts genuinely mutate
shared state and cannot move the mutation into `Because`; state the reason locally.

`Specification.InitializeAsync()` is idempotent for a single instance. It does not make
an ineligible class shared or cache instances across classes.

## Native runner teardown outcomes

Requires Cratis.Specifications.Runner 5.0.3 for separate class teardown results; check `dotnet list package`, or expect the older runner to mark every fact failed after teardown fails.

A failing `Destroy`, disposal or scope end produces one separate failed result
for the specification class and fails the run. Completed facts keep their own
outcomes, as on xUnit/NUnit. Select the teardown result to rerun its class; passing
facts do not imply the class or run passed.

## CRSPEC0008

`CRSPEC0008` is an error by default: a `should_` fact that assigns to or increments an
instance field or property of the specification, or calls `Add`, `AddRange`, `Remove`,
`RemoveAt`, `Clear` or `Insert` on one, would make the result depend on fact order.
Requires Specifications 5.0.2 for Received.InOrder verification recognition; check `dotnet list package`, or upgrade before relying on this diagnostic exception.

Calls inside NSubstitute `Received.InOrder(...)` delegates are recognized as
verification, but assignments and increments there still mutate state and are
reported. Theories, per-fact classes/assemblies, immutable collection calls and
collection calls whose return value is used are excluded.

Since Specifications 5.2.0, `CRSPEC0008` also reports indirect changes it can prove:
writes through nested members and indexers (also `?.`), `Dispose`/`DisposeAsync` on a
shared field, reconfiguring a shared NSubstitute substitute (`Returns`, `Throws`,
`When`, `ClearReceivedCalls`), and calls to a method in the same file that does any of
these. The warning `CRSPEC0012` reports calls that usually change state, judged by
name: `Execute`/`Handle`, destructive reads (`Take`, `Dequeue`, `Pop`, `MoveNext`) and
`Push`, `Enqueue`, `TryAdd`, `Reset`, or `Add`/`Remove`/`Clear` on a collection reached
through a field. Helpers in other files (such as a `given` context) and Moq `Setup`
are not followed, so audit those classes by hand before turning the shared lifecycle
back on. Move actual mutations into `Establish` or `Because`:

```csharp
public class when_registering : Specification
{
    List<string> _registered;

    void Establish() => _registered = [];
    void Because() => _registered.Add("first");

    [Fact] void should_have_one_registration() => _registered.Count.ShouldEqual(1);
}
```

Do not lower the severity to pass the gate. The analyzer is not a complete mutation
audit: also inspect helper calls, nested/indexer writes, command execution,
destructive reads, disposal and substitute reconfiguration inside facts. Awaiting
a read-only assertion is not itself a mutation.

## Keep shared execution on

Do not opt the assembly or project out for a few mutating facts. Remove blanket
opt-outs only after auditing fact mutations; move actions into `Because` where
possible and put `[PerFactSpecificationLifecycle]` on the remaining concrete
classes, with a reason. Compare discovered/passed/failed/skipped counts and TRX
durations before and after with unchanged concurrency. A clean analyzer run alone
does not prove that all facts are read-only.

Read [running-specs.md](./running-specs.md) for the executable loop and the 4.x → 5.0
checklist.
