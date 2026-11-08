```csharp
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Sinks;

[Graph("People")]
[Sink("people")]
public class PeopleGraph : IGraphProjection
{
    public void Define(IGraphProjectionBuilder builder)
    {
        // Nodes and edges of the graph.
    }
}
```
