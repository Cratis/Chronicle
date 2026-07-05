```csharp
try
{
    var result = await projections.Query("""
        projection Orders
          from OrderPlaced
        """);
}
catch (UnableToQueryProjection ex)
{
    Console.WriteLine(ex.Message);
}
```
