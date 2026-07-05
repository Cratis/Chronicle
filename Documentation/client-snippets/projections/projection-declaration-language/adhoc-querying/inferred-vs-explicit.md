```csharp
// Inferred — schema derived from OrderPlaced and OrderShipped event properties
var inferred = await projections.Query("""
    projection Orders
      from OrderPlaced
      from OrderShipped
    """);

// Explicit — schema comes from the registered 'PdlOrderReadModel' type
var explicitResult = await projections.Query("""
    projection Orders => PdlOrderReadModel
      from OrderPlaced
      from OrderShipped
    """);
```
