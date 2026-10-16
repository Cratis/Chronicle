<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/compatibility-check.md -->
## The client and the server check each other

Source: `Cratis/Chronicle`: `ChronicleConnection.cs`,
`ChronicleConnectionStringBuilder.cs`, `ChronicleOptions.cs` and
`Source/Kernel/Compatibility/`.

`chronicle://host:35000/?skipCompatibilityCheck=true` or
`ChronicleOptions.SkipCompatibilityCheck = true` skips the check; either input
is sufficient, including for a standalone `ChronicleClient`. Use this only for
a known-safe skew against a kernel that cannot yet be redeployed: it hides
**genuine incompatibilities too**. This is independent of `skipTlsValidation`.
Requires Chronicle kernel 19.0.1: it orders contract comparison by protocol version, so added
fields in a newer client do not by themselves make an older kernel incompatible.

The compatibility check is a server-side RPC, and **the client
runs it automatically inside `Connect()`** —
`Source/Clients/Connections/ChronicleConnection.cs` calls
`CheckCompatibility`, sending the client type, client version, protocol
version, and the descriptor set its contracts package was built with.
The rationale is at: Chronicle has clients in four languages and only
some can build a descriptor set at runtime, so each ships the one it was built
with and the server does the single comparison.

Behavior you can rely on:

- A server too old to have the RPC answers `Unimplemented`, and the client falls
  back to the previous client-side exchange — upgrading the client
  does not silently drop the check.
- Any other transport error is **logged and ignored**, on the stated
  reasoning that failing to ask says nothing about whether the two sides agree.
- A genuine mismatch throws `IncompatibleServerException`
  (`Source/Clients/Connections/IncompatibleServerException.cs`) whose message
  names the server address, its version, its protocol version, and the specific
  incompatibilities.

The client identifies itself as `".NET"`
(`Source/Clients/Connections/ChronicleClientIdentity.cs`) with its assembly
informational version and the contracts protocol version.
