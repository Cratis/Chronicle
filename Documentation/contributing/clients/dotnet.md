# .NET Client

The .NET client is at the root of most clients. The philosophy is to have
a very simple API to get started and at the same time giving the developers
full control over everything. This means that for the simplest of use cases
the .NET client will come with defaults that will get you up and running fast,
but all of the defaults needs to be overridable.

An example of this is the `ChronicleClient` itself. You can create an instance
with only the `URI` for the server and you're good to go. You can then call
the discover method on it for discovering all artifacts, or you can go and
individually discover only the things you want it to discover. In addition
you can even override the discovery mechanism by implementing your own the
`IClientArtifactsProvider`.

With this we philosophy we are providing an easy way to get started and then
an easy way to override and have full control.

## Repack

The .NET client is repacked. It hides the [contracts](./contracts.md) and
also includes the `Connection` project.
