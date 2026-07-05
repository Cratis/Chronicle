```csharp
public class PdlExpressionsUserReadModel
{
    public string Name { get; set; } = string.Empty; // Requires string
    public int LoginCount { get; set; }              // Requires number
    public bool IsActive { get; set; }               // Requires boolean
    public DateTime CreatedAt { get; set; }           // Requires timestamp
}
```
