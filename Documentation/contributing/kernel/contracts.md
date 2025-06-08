# Contracts

The Contracts project contains all gRPC definitions for data and services, representing the complete protocol of the **Kernel**.
Contracts are defined as C# types: data is represented using classes, and services as interfaces.

We use [protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) to enable a *code-first* approach to defining protobuf contracts.

The implementation of these contracts resides in the **Kernel**. For more details, see the [services documentation](../kernel/services.md).

## Attributes

[protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) supports defining contracts using annotations from `System.ServiceModel`
as well as those from `Protobuf` and `Protobuf.Grpc`. We use the latter.

For data types, decorate the class with `[ProtoContract]` and each property with `[ProtoMember(<id>)]`, where **id** is a sequential number
indicating the field's position in the contract.

Example:

```csharp
[ProtoContract]
public class AppendRequest
{
    [ProtoMember(1)]
    public string EventStore { get; set; }

    [ProtoMember(2)]
    public string Namespace { get; set; }

    [ProtoMember(3)]
    public string EventSequenceId { get; set; }
}
```

Always add new properties at the end, without reordering existing ones.

For service contracts, use the `[Service]` and `[Operation]` attributes.

Example:

```csharp
[Service]
public interface IEventSequences
{
    /// <summary>
    /// Append an event to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="AppendResponse"/>.</returns>
    [Operation]
    Task<AppendResponse> Append(AppendRequest request, CallContext context = default);
}
```

## Streaming APIs

Some APIs require streaming data from client to server, server to client, or both. We use the `System.Reactive` type `IObservable<>` for streaming, as it is intuitive,
consistent with our implementation, and provides expressive power through the fluent interfaces of `System.Reactive`.

Example:

```csharp
[Service]
public interface IReactors
{
    [Operation]
    IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default);
}
```
