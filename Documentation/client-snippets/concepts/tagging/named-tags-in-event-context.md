```csharp
using System.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

public class TaggingCatalogImportTracker : IReactor
{
    public Task Imported(TaggingCatalogItemImported @event, EventContext context)
    {
        // Names and values compare exactly (ordinal, case-sensitive).
        // A name can carry more than one value, so read every match.
        var batchIds = context.NamedTags
            .Where(tag => tag.Name.Value == "import-batch")
            .Select(tag => tag.Value)
            .ToArray();

        Console.WriteLine($"{@event.Sku} imported in batch(es) {string.Join(", ", batchIds)}");
        return Task.CompletedTask;
    }
}
```
