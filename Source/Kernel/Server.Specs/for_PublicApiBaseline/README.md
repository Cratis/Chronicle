# Contracts public API baseline

This offline gate snapshots every exported type defined in the assembly containing
`Cratis.Chronicle.Contracts.IServices`. It does not start Program, discover Arc
artifacts, connect to storage, invoke handlers, or inspect dependency assemblies.
The sample contracts are renderer inputs only, never host registrations.

## Format and coverage

Each declaration or selected attribute occupies one ordinally sorted line, with
LF endings and a final newline. Identities include declaring types and signatures;
there are no assembly versions, metadata tokens, paths, or timestamps. Reference
nullability uses `?` (nullable), `!` (not null), and `~` (unknown) where reflection
provides it. Generic parameters include their position and declaring scope.

The renderer includes declared public members, constructors, record-generated
methods/operators, inheritance, generic constraints, accessor visibility, init
modifiers, optional defaults, constants, enum values, and selected contract,
wire, obsolete, required, and nullability attributes. Nonpublic accessors of a
public property/event are included. It reads CustomAttributeData without
constructing attributes. Unsupported member kinds, function pointers, constructed
generic declaration inputs, empty inventories, and duplicate lines fail loudly.
Extend the renderer and its planted specs before approving a novel signature.

Every difference fails, including additions. This forces review and ratchets
new API into protection, but is not universal semantic-versioning or binary
compatibility proof. In particular, additive abstract interface obligations may
break implementers.

## Deliberate bootstrap and regeneration

1. Build `Source/Kernel/Server.Specs/Server.Specs.csproj`, then run its filtered
   tests with `--filter FullyQualifiedName~Cratis.Chronicle.Server.for_PublicApiBaseline`.
   Use the same configuration for the build and test; do not use stale assemblies.
2. On mismatch the test writes `KernelContracts.public-api.received.txt` under
   `AppContext.BaseDirectory` (the test output directory), never under a source
   or working-directory-relative path. The failure prints its absolute path and
   the first twenty removed and added lines, with full counts. Missing, empty,
   duplicate, unsorted, and noncanonical baselines also fail.
3. Review the complete diff against that received file, not only the bounded
   summary. For initial bootstrap, review the complete received snapshot against
   the current Contracts assembly. Do not hand-invent missing declarations.
4. After deliberate API review, copy the generated file using the printed path:

   ```sh
   cp <printed received path> Source/Kernel/Server.Specs/for_PublicApiBaseline/KernelContracts.public-api.txt
   ```

5. Rebuild to embed the reviewed resource and rerun the filtered tests. Require a
   nonzero discovered count and a pass, then rerun to confirm deterministic output.
   If another received file is produced, compare its bytes before accepting it.
   Check production/generated paths for incidental build output changes; do not
   blindly discard another writer's changes.

Tests and CI never rewrite the committed baseline or read a working-directory-relative
file. Successful comparisons do not write a received file. Later updates require deliberate API review. P1/P2 entitlement changes
must retain old declaration lines; do not regenerate removals or signature
changes away without explicit compatibility authority. Review additions too.

## Separate wire gate

The existing WireCompatibility CI regenerates protobuf/descriptors, checks stale
output, and compares released minors of the current major across clients. This
snapshot instead protects CLR names, constructors, accessors, optional defaults,
nullability, signatures, and facade shape against a reviewed local baseline.
ProtoMember indexes intentionally overlap; this gate does not replace or alter
released-wire compatibility checks or prove authorization enforcement.
