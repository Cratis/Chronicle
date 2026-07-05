```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class OrderReactor(string userId) : IReactor, ICanProvideSubject
{
    public Subject GetSubject() => new(userId);
}
```
