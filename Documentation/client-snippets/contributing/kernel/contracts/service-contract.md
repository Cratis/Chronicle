```csharp
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

[Service]
public interface IContributingKernelEventSequences
{
    /// <summary>
    /// Append an event to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="ContributingKernelAppendRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The response.</returns>
    [Operation]
    Task<ContributingKernelAppendResponse> Append(ContributingKernelAppendRequest request, CallContext context = default);
}

public class ContributingKernelAppendResponse
{
}
```
