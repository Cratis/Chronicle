# Contracts

The Contracts project contains all gRPC definitions for data and services, representing the complete protocol of the **Kernel**.
Contracts are defined as C# types: data is represented using classes, and services as interfaces.

We use [protobuf-net.Grpc](https://github.com/protobuf-net/protobuf-net.Grpc) to enable a *code-first* approach to defining protobuf contracts.

The implementation of these contracts resides in the **Kernel**. For more details, see the [services documentation](./services.md).

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

## SerializableDateTimeOffset

Protobuf does not natively support `DateTimeOffset`, so we provide `SerializableDateTimeOffset` in the `Contracts.Primitives` namespace.
This type serializes a `DateTimeOffset` as ticks and UTC offset in minutes, both of which are protobuf-compatible.

`SerializableDateTimeOffset` provides implicit conversion operators to and from `DateTimeOffset`, making it transparent to use in your code.

**Always use `SerializableDateTimeOffset` instead of `DateTimeOffset` in contract definitions.**

Example:

```csharp
[ProtoContract]
public class User
{
    [ProtoMember(1)]
    public string Username { get; set; }

    [ProtoMember(2)]
    public SerializableDateTimeOffset CreatedAt { get; set; } = new();

    [ProtoMember(3)]
    public SerializableDateTimeOffset? LastModifiedAt { get; set; }
}
```

The implicit conversions allow natural usage:

```csharp
var user = new User
{
    Username = "john",
    CreatedAt = DateTimeOffset.UtcNow  // Implicit conversion from DateTimeOffset
};

DateTimeOffset created = user.CreatedAt;  // Implicit conversion to DateTimeOffset
```

## OneOf

When you need a contract property that can hold one of several different types (a discriminated union), use the `OneOf<T0, T1, ...>` generic types
from the `Contracts.Primitives` namespace. These support 2, 3, or 4 type parameters.

Each `OneOf` instance stores the value in one of its `Value0`, `Value1`, etc. properties, with the others set to `null`. The `Value` property
returns whichever value is set, or throws `NoValueSetForOneOf` if none are set.

Example with a service returning either a result or an error:

```csharp
[Service]
public interface IJobs
{
    [Operation]
    Task<OneOf<Job, JobError>> GetJob(GetJobRequest request, CallContext context = default);
}
```

Example with a message containing different content types:

```csharp
[ProtoContract]
public class ReactorMessage
{
    [ProtoMember(1)]
    public OneOf<RegisterReactor, ReactorResult> Content { get; set; }
}
```

Usage on the client side:

```csharp
var result = await jobsService.GetJob(request);

if (result.Value0 is not null)
{
    // Handle Job
    var job = result.Value0;
}
else if (result.Value1 is not null)
{
    // Handle JobError
    var error = result.Value1;
}

// Or use the Value property to get whichever is set
object value = result.Value;
```

