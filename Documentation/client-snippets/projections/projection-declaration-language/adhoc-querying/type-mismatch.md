```csharp
// This declaration will throw UnableToQueryProjection:
// OrderPlaced.value is string, but OrderShipped.value is int
var result = await projections.Query("""
    projection Bad
      from OrderPlaced   // value: string
      from OrderShipped  // value: int  → incompatible types
    """);
```
