```csharp
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

[ProtoContract]
public class ContributingKernelReactorMessage
{
    [ProtoMember(1)]
    public string Content { get; set; } = string.Empty;
}

[ProtoContract]
public class ContributingKernelEventsToObserve
{
    [ProtoMember(1)]
    public string EventType { get; set; } = string.Empty;
}

[Service]
public interface IContributingKernelReactors
{
    [Operation]
    IObservable<ContributingKernelEventsToObserve> Observe(IObservable<ContributingKernelReactorMessage> messages, CallContext context = default);
}
```
