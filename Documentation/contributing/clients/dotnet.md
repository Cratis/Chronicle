# .NET Client

The .NET client serves as the foundation for most Chronicle clients. Its design philosophy is to
offer a streamlined API for quick onboarding, while also empowering developers with full control
and extensibility. Out of the box, the client provides sensible defaults to help you get started
rapidly, but every default is fully overridable to suit advanced scenarios.

For example, you can instantiate a `ChronicleClient` with just the server `URI` and immediately
begin working. You may use the built-in discovery method to automatically discover all artifacts,
or selectively discover only those you need. If you require custom behavior, you can override the
discovery mechanism entirely by implementing your own `IClientArtifactsProvider`.

This approach ensures an easy entry point for new users, while also supporting advanced
customization for those who need it.

## Repack

The .NET client is repacked. It hides the [contracts](../kernel/contracts.md) and
also includes the `Connection` project.
