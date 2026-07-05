```csharp
using Cratis.Chronicle.Events;

[EventType("f47ac10b-58cc-4372-a567-0e02b2c3d480")]
public record Chr0021UserRegisteredWithInit
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
```
