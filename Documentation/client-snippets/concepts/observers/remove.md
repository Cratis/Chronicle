```csharp
var result = await eventStore.Observers.Remove("the-observer");
if (!result.IsRemoved)
{
    Console.WriteLine($"Not removed: {result.Outcome} in namespace {result.BlockingNamespace}");
}
```
