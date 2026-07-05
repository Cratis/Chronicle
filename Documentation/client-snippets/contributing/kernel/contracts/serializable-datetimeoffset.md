```csharp
using ProtoBuf;
using Cratis.Chronicle.Contracts.Primitives;

[ProtoContract]
public class ContributingKernelUser
{
    [ProtoMember(1)]
    public string Username { get; set; } = string.Empty;

    [ProtoMember(2)]
    public SerializableDateTimeOffset CreatedAt { get; set; } = new();

    [ProtoMember(3)]
    public SerializableDateTimeOffset? LastModifiedAt { get; set; }
}
```
