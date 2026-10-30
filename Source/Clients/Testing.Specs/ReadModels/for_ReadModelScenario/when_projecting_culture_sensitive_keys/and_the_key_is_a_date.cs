// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_culture_sensitive_keys;

public class and_the_key_is_a_date : Specification
{
    ReadModelScenario<DateSummary> _scenario;
    EventSourceId<DateTimeOffset> _id;

    void Establish()
    {
        _id = new(new DateTimeOffset(2026, 10, 23, 14, 15, 16, TimeSpan.FromHours(2)));
        _scenario = new ReadModelScenario<DateSummary>()
            .WithProjection(builder => builder.From<DateRecorded>(from => from.UsingKey(@event => @event.Id)));
    }

    async Task Because()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
            await _scenario.ProcessEventsFor(EventSourceId.New(), [new DateRecorded(_id.TypedValue, "Recorded")]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_find_the_read_model_by_typed_id() => _scenario.InstanceForEventSourceId(_id)!.Value.ShouldEqual("Recorded");
    [Fact] void should_preserve_the_date_key() => _scenario.InstanceForEventSourceId(_id)!.Id.ShouldEqual(_id.TypedValue);

    [EventType]
    public record DateRecorded(DateTimeOffset Id, string Value);

    public record DateSummary(DateTimeOffset Id, string Value);
}
