```csharp
var scenario = new ReadModelScenario<OrderSummary>();

foreach (var substitution in scenario.Substitutions)
{
    Console.WriteLine($"{substitution.Layer}: {substitution.Shape} — {substitution.Consequence}");
}
```
