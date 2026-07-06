```csharp
using Cratis.Chronicle.Events;

[EventType("f47ac10b-58cc-4372-a567-0e02b2c3d479")]
public class Chr0021UserRegistered // CHR0021: mutable class
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```
