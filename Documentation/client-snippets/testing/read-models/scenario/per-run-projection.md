```csharp
var scenario = new ReadModelScenario<MyReadModel>(null, new Defaults(artifacts))
    .WithProjection(builder => builder.From<MyEvent>(from =>
        from.Set(model => model.Name).To(e => e.Name)));
await scenario.Given.ForEventSource(id).Events(new MyEvent("example"));
var result = scenario.Instance;
```
