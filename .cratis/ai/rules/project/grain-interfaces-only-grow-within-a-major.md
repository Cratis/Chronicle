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

Within a major release, for every grain interface in `Source/Kernel/Core`:

- **Never change or remove a method** - not a parameter, not its type, not the return type. Add a new method beside it
  and have the old one forward to it. Remove the old one only in a major release.
- **Raise `[Version]` when an interface gains a method.** It routes a call that needs the new method to a silo that has
  it, instead of to an activation on an older silo. Add `using Orleans.CodeGeneration;` at the top of the file and
  write `[Version(n)]`; qualifying it in place fails, because inside a `Cratis.*` namespace `Orleans` resolves to
  `Cratis.Orleans`.
- **Never move or rename a grain interface, grain class or a type a custom codec writes the name of** (the
  `Cratis.Orleans.Serialization` codecs). The name is on the wire; moving `ConceptSerializer` broke 19.4 to 19.13 (#4325).
- **Types passed between silos only gain properties.** Renaming, retyping or removing one is not caught by any gate.

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
