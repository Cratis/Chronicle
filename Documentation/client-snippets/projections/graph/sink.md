```csharp
services.Configure<ChronicleOptions>(options =>
{
    // Neo4j is the default. The InMemoryGraph sink keeps the graph in the kernel's memory.
    options.DefaultGraphSinkTypeId = WellKnownSinkTypes.InMemoryGraph;
});
```
