```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    namespace given
    {
        public class a_unique_value_reactor_context(ChronicleInProcessFixture fixture) : Specification(fixture)
        {
            public IEventAppendCollection AppendedEventsCollector = null!;

            public override IEnumerable<Type> EventTypes => [typeof(UniqueValueRecorded), typeof(UniqueValueFollowUp)];
            public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueValueFollowUpConstraint)];
            public override IEnumerable<Type> Reactors => [typeof(UniqueValueReactor)];

            protected override void ConfigureServices(IServiceCollection services) =>
                services.AddSingleton<UniqueValueReactor>();

            void Destroy() => AppendedEventsCollector?.Dispose();
        }
    }
}
```
