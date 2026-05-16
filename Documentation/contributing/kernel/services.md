# Services

All gRPC service implementations for our [contracts](./contracts.md) reside in the `Services` project.

These services are owned by the `Kernel` and must remain internal. Every service implementation should
be marked as `internal`, since integration testing can host the client and kernel in the same process.
This is required by our [internalization](../clients/internalization.md) process for client assemblies.

Service implementations are built on top of the Grains exposed by the Kernel.
