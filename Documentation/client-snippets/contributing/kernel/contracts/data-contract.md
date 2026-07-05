```csharp
using ProtoBuf;
using ProtoBuf.Grpc;

[ProtoContract]
public class ContributingKernelAppendRequest
{
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string EventSequenceId { get; set; } = string.Empty;
}
```
