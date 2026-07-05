```csharp
using Cratis.Chronicle.Events;

[EventType]
public record CamelCasingUserRegistered(
    string FirstName,
    string LastName,
    string EmailAddress,
    DateTime RegistrationDate);
```
