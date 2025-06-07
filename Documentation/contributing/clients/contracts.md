# Contracts

[services](../kernel/services.md)


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
