// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_culture_sensitive_keys;

public class and_the_key_is_decimal : Specification
{
    ReadModelScenario<DecimalSummary> _scenario;
    EventSourceId<decimal> _id;

    void Establish()
    {
        _id = new(123.45m);
        _scenario = new ReadModelScenario<DecimalSummary>()
            .WithProjection(builder => builder.From<DecimalRecorded>(from => from.UsingKey(@event => @event.Id)));
    }

    async Task Because()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
            await _scenario.ProcessEventsFor(EventSourceId.New(), [new DecimalRecorded(_id.TypedValue, "Recorded")]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_find_the_read_model_by_typed_id() => _scenario.InstanceForEventSourceId(_id)!.Value.ShouldEqual("Recorded");
    [Fact] void should_preserve_the_decimal_key() => _scenario.InstanceForEventSourceId(_id)!.Id.ShouldEqual(_id.TypedValue);

    [EventType]
    public record DecimalRecorded(decimal Id, string Value);

    public record DecimalSummary(decimal Id, string Value);
}
