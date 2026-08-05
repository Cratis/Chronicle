```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record Chr0041TemperatureRead(double Degrees);

// Warning CHR0041: '[FilterEventsByTag]' on projection 'Chr0041LatestTemperature' has no
// effect - a projection observes every event of the types it declares and cannot filter
// on event metadata. Readings from every sensor land in this read model, not just sensor-a's.
[FilterEventsByTag("sensor-a")]
[FromEvent<Chr0041TemperatureRead>]
public record Chr0041LatestTemperature(
    [Key] Guid Id,
    double Degrees);
```
