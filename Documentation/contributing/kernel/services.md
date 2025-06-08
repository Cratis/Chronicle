# Services

All implementation of gRPC services for [contracts](./contracts.md) are found
in the `Services` project.

It is owned by the `Kernel` and should always be kept internal.

All service implementations need to be marked `internal` as they are also
merged into the `DotNET.InProcess` client. This is due to how we [internalize](../clients/internalization.md)
merged assemblies for our clients.

Services build on top of the Grains that are exposed by the Kernel.
