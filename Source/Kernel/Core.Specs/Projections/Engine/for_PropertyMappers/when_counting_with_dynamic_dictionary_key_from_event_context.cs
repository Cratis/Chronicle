// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_counting_with_dynamic_dictionary_key_from_event_context : Specification
{
    const string FirstEventTypeId = "02405794-91e7-4e4f-8ad1-f043070ca297";
    const string SecondEventTypeId = "9a4f1c9b-7e5e-4b90-9d0a-9f6b1f8c6d21";

    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.Count(
            new TypeFormats(),
            "eventCountByType.$eventContext.eventType.id",
            new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int64" });
    }

    void Because()
    {
        _propertyMapper(EventOfType(FirstEventTypeId), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(EventOfType(FirstEventTypeId), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(EventOfType(SecondEventTypeId), _target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_count_the_first_event_type_under_its_own_key() =>
        ((long)((IDictionary<string, object>)((dynamic)_target).eventCountByType)[FirstEventTypeId]).ShouldEqual(2);

    [Fact] void should_count_the_second_event_type_under_its_own_key() =>
        ((long)((IDictionary<string, object>)((dynamic)_target).eventCountByType)[SecondEventTypeId]).ShouldEqual(1);

    [Fact] void should_use_int64_as_the_dictionary_value_type() =>
        ((IDictionary<string, object>)((dynamic)_target).eventCountByType)[FirstEventTypeId].ShouldBeOfExactType<long>();

    static AppendedEvent EventOfType(string eventTypeId) => new(
        new(
            new(eventTypeId, 1),
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
        new ExpandoObject());
}
