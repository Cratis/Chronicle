```csharp
public class PdlGroupReadModel
{
    public string Name { get; set; } = string.Empty;
    public List<PdlGroupMember> Members { get; set; } = new();
}

public class PdlGroupMember
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```
