// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDateTimes;

/// <summary>
/// A <c language="json">date-time</c> value with an explicit offset is now held as a <see cref="DateTimeOffset"/>, where it used to be
/// held as the host's local <see cref="DateTime"/>. A projection must compute exactly what it computed before for such
/// a value, and keep computing what it did for a <c language="json">date-time-offset</c> value.
/// </summary>
public class when_resolving_date_time_values_for_a_projection : Specification
{
    static readonly DateTimeOffset _moment = DateTimeOffset.Parse("2025-01-02T03:04:05.1234567+05:30");
    static readonly EventType _eventType = new("02405794-91e7-4e4f-8ad1-f043070ca297", 1);

    EventValueProviderExpressionResolvers _resolvers;
    ProjectionDateTimes _dateTimes;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _dateTimes = new([new EventTypeSchema(_eventType, EventTypeOwner.Client, EventTypeSource.Code, EventSchema())]);
    }

    [Theory]
    [InlineData("date-time")]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData(null)]
    void should_resolve_a_date_time_value_as_it_did_when_it_was_held_in_the_local_time_of_the_host(string? format)
    {
        var target = new JsonSchemaProperty { Type = JsonObjectType.String, Format = format };

        var result = Resolve(target, "Moment", _dateTimes.ForProjection(Event(_moment)));

        result.ShouldEqual(Resolve(target, "Moment", Event(_moment.LocalDateTime)));
    }

    [Theory]
    [InlineData("date-time")]
    [InlineData("date-time-offset")]
    [InlineData("date")]
    [InlineData(null)]
    void should_resolve_a_date_time_offset_value_as_it_did_before(string? format)
    {
        var target = new JsonSchemaProperty { Type = JsonObjectType.String, Format = format };
        var @event = Event(_moment);

        var result = Resolve(target, "Instant", _dateTimes.ForProjection(@event));

        result.ShouldEqual(Resolve(target, "Instant", @event));
    }

    [Fact]
    void should_leave_an_event_without_date_time_values_to_change_as_it_is()
    {
        var @event = Event(_moment.LocalDateTime);
        _dateTimes.ForProjection(@event).ShouldBeSame(@event);
    }

    [Fact]
    void should_leave_an_event_of_a_type_without_a_schema_as_it_is()
    {
        var @event = Event(_moment);
        @event = @event with { Context = @event.Context with { EventType = new("9a0a3f41-4f34-4c8a-9f1e-0d6d1f0d0c1e", 1) } };
        _dateTimes.ForProjection(@event).ShouldBeSame(@event);
    }

    object Resolve(JsonSchemaProperty target, string property, AppendedEvent @event) =>
        _resolvers.Resolve(target, property)(@event);

    static AppendedEvent Event(object moment)
    {
        var content = new ExpandoObject();
        var values = (IDictionary<string, object?>)content;
        values["Moment"] = moment;
        values["Instant"] = _moment;

        return new(
            new(
                _eventType,
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            content);
    }

    static JsonSchema EventSchema() => JsonSchema.FromJson("""
        {
            "type": "object",
            "properties": {
                "Moment": { "type": "string", "format": "date-time" },
                "Instant": { "type": "string", "format": "date-time-offset" }
            }
        }
        """);
}
