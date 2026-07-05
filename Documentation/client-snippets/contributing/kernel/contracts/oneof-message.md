```csharp
using ProtoBuf;
using Cratis.Chronicle.Contracts.Primitives;

[ProtoContract]
public class ContributingKernelRegisterReactor
{
    [ProtoMember(1)]
    public string ReactorId { get; set; } = string.Empty;
}

[ProtoContract]
public class ContributingKernelReactorResult
{
    [ProtoMember(1)]
    public bool Success { get; set; }
}

[ProtoContract]
public class ContributingKernelReactorOneOfMessage
{
    [ProtoMember(1)]
    public OneOf<ContributingKernelRegisterReactor, ContributingKernelReactorResult> Content { get; set; } = new();
}
```
