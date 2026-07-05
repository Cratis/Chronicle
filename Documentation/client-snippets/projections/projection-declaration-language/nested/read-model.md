```csharp
public class PdlSliceReadModel
{
    public string Name { get; set; } = string.Empty;
    public PdlCommandItem? Command { get; set; } // nullable — null until set
}

public class PdlCommandItem
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
}
```
