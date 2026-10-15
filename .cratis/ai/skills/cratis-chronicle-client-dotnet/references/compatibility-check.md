<!-- cratis-ai-managed: skills/cratis-chronicle-client-dotnet/references/compatibility-check.md -->
## The client and the server check each other

Verified at **Cratis/Chronicle v19.50.0**, `ChronicleConnection.cs`,
`ChronicleConnectionStringBuilder.cs`, `ChronicleOptions.cs` and
`Source/Kernel/Compatibility/`.

`chronicle://host:35000/?skipCompatibilityCheck=true` or
`ChronicleOptions.SkipCompatibilityCheck = true` skips the check; either input
is sufficient, including for a standalone `ChronicleClient`. Use this only for
a known-safe skew against a kernel that cannot yet be redeployed: it hides
**genuine incompatibilities too**. This is independent of `skipTlsValidation`.
Since 19.0.1 the kernel orders contract comparison by protocol version, so added
fields in a newer client do not by themselves make an older kernel incompatible.

Since Chronicle 17 the compatibility check is a server-side RPC, and **the client
runs it automatically inside `Connect()`** —
`Source/Clients/Connections/ChronicleConnection.cs:310` calls
`CheckCompatibility` (`:414`), sending the client type, client version, protocol
version, and the descriptor set its contracts package was built with (`:420-426`).
The rationale is at `:409-413`: Chronicle has clients in four languages and only
some can build a descriptor set at runtime, so each ships the one it was built
with and the server does the single comparison.

Behavior you can rely on:

- A server too old to have the RPC answers `Unimplemented`, and the client falls
  back to the previous client-side exchange (`:428-434`) — upgrading the client
  does not silently drop the check.
- Any other transport error is **logged and ignored** (`:435-441`), on the stated
  reasoning that failing to ask says nothing about whether the two sides agree.
- A genuine mismatch throws `IncompatibleServerException`
  (`Source/Clients/Connections/IncompatibleServerException.cs:10`) whose message
  names the server address, its version, its protocol version, and the specific
  incompatibilities (`:443-449`).

The client identifies itself as `".NET"`
(`Source/Clients/Connections/ChronicleClientIdentity.cs:22`) with its assembly
informational version (`:27`) and the contracts protocol version (`:32`).
