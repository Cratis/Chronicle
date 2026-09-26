```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
[Tag("catalog")]
public record TaggingCatalogItemImported(string Sku, string Title);

public class TaggingCatalogImportService(IEventLog eventLog)
{
    public Task<AppendResult> Import(EventSourceId itemId, string sku, string title, string batchId) =>
        // Plain tags: ["catalog", "import"]
        // Named tags: import-batch = <batchId>, source-system = erp
        eventLog.Append(
            itemId,
            new TaggingCatalogItemImported(sku, title),
            namedTags: [new NamedTag("import-batch", batchId), new NamedTag("source-system", "erp")],
            tags: ["import"]);
}
```
