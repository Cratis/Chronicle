# Contracts

The contracts project holds all the gRPC definitions for data and services, representing the
full protocol of the **Kernel**. They are defined as C# types, where data
is represented using classes and services as interfaces.

It leverages [protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) to be able
to have this *code-first* approach to defining protobuf contracts.

The implementation of the contracts belongs to the **Kernel**, see the [services documentation](../kernel/services.md)
for more details.

## Attributes

The [protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) supports defining
the contracts using annotations found in `System.ServiceModel` and the ones found in `Protobuf` and `Protobuf.Grpc`.
We leverage the latter annotations.

For a data type, this means using the `[ProtoContract]` on the the type and the `[ProtoMember(<id>)]` on
properties where the **id** is an incremental number that identifies the position within the contract.

Following is an example:

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

Additional properties should always be added at the end without reordering existing.

For service contracts, we leverage the `[Service]` and `[Operation]` attributes.

The following is an example of use.

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

Some APIs need to stream data from client to server or server to client or both ways.
We leverage the `System.Reactive` type `IObservable<>` for this, as we find that more
intuitive and is also more consistent with how we implement throughout. It also
gives you more expressive power when consuming these as you can then leverage the
full power of `System.Reactive` and its fluent interfaces.

```csharp
[Service]
public interface IReactors
{
    [Operation]
    IObservable<EventsToObserve> Observe(IObservable<ReactorMessage> messages, CallContext context = default);
}
```
