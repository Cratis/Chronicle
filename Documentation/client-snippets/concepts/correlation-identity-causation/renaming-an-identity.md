```csharp
using Cratis.Chronicle;

public static class RenamingAnIdentity
{
    public static async Task Rename(IEventStore eventStore)
    {
        await eventStore.Identities.Rename("subject-42", "Jane Austen");
    }
}
```
