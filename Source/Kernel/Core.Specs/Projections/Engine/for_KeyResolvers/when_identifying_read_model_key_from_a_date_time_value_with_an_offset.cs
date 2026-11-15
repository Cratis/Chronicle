// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

/// <summary>
/// A <c language="json">date-time</c> value with an explicit offset used to be held as the host's local <see cref="DateTime"/>.
/// A projection keyed on such a value must resolve the same key as before.
/// </summary>
public class when_identifying_read_model_key_from_a_date_time_value_with_an_offset : Specification
{
    static readonly DateTimeOffset _moment = DateTimeOffset.Parse("2025-01-02T03:04:05.1234567+05:30");
    static readonly EventType _eventType = new("02405794-91e7-4e4f-8ad1-f043070ca297", 1);

    KeyResolver _resolver;
    ProjectionDateTimes _dateTimes;
    object _key;
    object _keyAsBefore;

    void Establish()
    {
        _resolver = new KeyResolvers(NullLogger<KeyResolvers>.Instance).FromEventValueProvider(EventValueProviders.EventContent("moment"));
        _dateTimes = new([new EventTypeSchema(_eventType, EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromJson("""
            {"type":"object","properties":{"moment":{"type":"string","format":"date-time"}}}
            """))]);
    }

    async Task Because()
    {
        _key = await KeyFor(_dateTimes.ForProjection(Event(_moment)));
        _keyAsBefore = await KeyFor(Event(_moment.LocalDateTime));
    }

    [Fact] void should_resolve_the_key_as_before() => _key.ShouldEqual(_keyAsBefore);

    async Task<object> KeyFor(AppendedEvent @event)
    {
        var result = await _resolver(Substitute.For<IEventSequenceStorage>(), Substitute.For<ISink>(), @event);
        return ((ResolvedKey)result).Key.Value;
    }

    static AppendedEvent Event(object moment)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["moment"] = moment;
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
}
