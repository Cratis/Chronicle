```csharp
// Inferred — schema derived from OrderPlaced and OrderShipped event properties
var inferred = await projections.Query("""
    projection Orders
      from OrderPlaced
      from OrderShipped
    """);

// Explicit — assuming PdlOrderReadModel is registered in the MyApp.ReadModels namespace
var explicitResult = await projections.Query("""
    projection Orders => MyApp.ReadModels.PdlOrderReadModel
      from OrderPlaced
      from OrderShipped
    """);
```
