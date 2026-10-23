---
applyTo: "Source/Kernel/**"
paths:
  - "Source/Kernel/**"
---

## Grain interfaces only grow within a major

During a rolling upgrade, silos of the previous release and of the new one share the Orleans cluster and call each
other's grains. Orleans identifies a grain method on the wire by a hash of its interface, name and **parameter
types**, so adding a parameter - even an optional one that every caller still compiles against - makes it a different
method. The other release cannot decode the call, and the new silo crashes on startup with
`TypeLoadException: Unable to resolve type alias ("inv", ..., "<hash>")`. That is how 19.70 to 19.106 broke (#5272).

Within a major release, for every grain interface in `Source/Kernel/Core` (anything deriving from `IGrainWith...Key`
or `IGrainObserver`):

| You want to | Do this |
| --- | --- |
| Add a method | Add it, and put `[Version(n)]` on that interface, one higher than its current version. No attribute means 0, so the first one is `[Version(1)]`; the current version of every interface is in `OrleansContracts.txt`. |
| Add a parameter, or change a parameter or return type | Do **not** edit the method. Add a new method beside it, raise `[Version]`, and have the old one forward to the new one. Remove the old one only in a major release. |
| Remove or rename a method | Not within a major. Keep it serving and remove it in the next major. |
| Rename or move a grain interface or grain class | Not within a major. The full name is on the wire. |
| Change only a method's body or XML docs | Nothing; the wire identity is unchanged. |

`[Version]` routes a call that needs the new method to a silo that has it, instead of to an activation on an older
silo. Raise it only on the interface that gained a method, never on every interface at once: raising it everywhere
makes every call from a new silo seek a new silo and moves every grain during a rollout. Write it with a `using`, not
qualified in place, because inside a `Cratis.*` namespace `Orleans` resolves to `Cratis.Orleans`:

```csharp
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Projections;

[Version(1)]
public interface IProjectionsManager : IGrainWithStringKey
{
    // Existing methods unchanged, including this one, which now forwards to the new overload.
    Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner = null, string? registrant = null, string? version = null);

    // New: the extra parameter is a new method, so the interface goes from version 0 to 1.
    Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version, bool replayOnChange);
}
```

Beyond the interfaces:

- **Never move or rename a type a custom codec writes the name of** (the `Cratis.Orleans.Serialization` codecs). The
  name is on the wire; moving `ConceptSerializer` broke 19.4 to 19.13 (#4325).
- **Types passed between silos only gain properties.** Renaming, retyping or removing one is not caught by any gate.
- **Treat a Cratis.Orleans major upgrade as a grain interface change.** Its job, job-step, worker and state-machine
  grain interfaces are outside this repository's `OrleansContracts.txt` and gate. Cratis.Orleans records and gates
  them the same way against its own latest release, so a minor or patch of it keeps them compatible; a major of it
  may break them, so take one only in a Chronicle major.

`Source/Kernel/Core/OrleansContracts.txt` records every grain interface, its version, each method's wire identity and
every grain class. It is generated, never hand-edited - except to delete a retained removed method in a major release.
After changing a grain interface, regenerate it and review the diff:

```bash
dotnet format Source/Kernel/Core/Core.csproj analyzers --severity info --diagnostics ORLEANS0016 ORLEANS0017 ORLEANS0018 ORLEANS0019 ORLEANS0020 ORLEANS0022 ORLEANS0023 ORLEANS0024
```

The Orleans contract analyzer fails the Core build when source and manifest disagree. The **Grain Compatibility**
workflow fails a pull request whose manifest is not purely additive against the latest release; a pull request labeled
`major` is exempt. The **RollingUpgrade** integration shard replaces a cluster of the latest release with this build one
node at a time. Full guidance: `Documentation/contributing/kernel/grain-interfaces.mdx`.
