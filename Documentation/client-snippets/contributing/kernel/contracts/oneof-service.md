```csharp
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using Cratis.Chronicle.Contracts.Primitives;

[ProtoContract]
public class ContributingKernelGetJobRequest
{
    [ProtoMember(1)]
    public string JobId { get; set; } = string.Empty;
}

[ProtoContract]
public class ContributingKernelJob
{
    [ProtoMember(1)]
    public string Id { get; set; } = string.Empty;
}

[ProtoContract]
public class ContributingKernelJobError
{
    [ProtoMember(1)]
    public string Message { get; set; } = string.Empty;
}

[Service]
public interface IContributingKernelJobs
{
    [Operation]
    Task<OneOf<ContributingKernelJob, ContributingKernelJobError>> GetJob(ContributingKernelGetJobRequest request, CallContext context = default);
}
```
