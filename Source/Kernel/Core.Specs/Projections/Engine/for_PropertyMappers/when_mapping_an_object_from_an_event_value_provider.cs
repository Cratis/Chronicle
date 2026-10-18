// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_mapping_an_object_from_an_event_value_provider : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    AppendedEvent _event;
    ExpandoObject _eventValue;
    ExpandoObject _result;
    PropertyDifference _difference;

    void Establish()
    {
        _result = new();
        _eventValue = new ExpandoObject();
        ((IDictionary<string, object?>)_eventValue)["Street"] = "Main street";
        _event = new(
            new(
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
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

        _propertyMapper = PropertyMappers.FromEventValueProvider("address", _ => _eventValue);
    }

    void Because() => _difference = _propertyMapper(_event, _result, ArrayIndexers.NoIndexers);

    object Mapped => ((IDictionary<string, object?>)_result)["address"]!;

    [Fact] void should_not_share_the_event_value_with_the_state() => Mapped.ShouldNotBeSame(_eventValue);
    [Fact] void should_not_share_the_event_value_with_the_difference() => _difference.Changed.ShouldNotBeSame(_eventValue);
    [Fact] void should_map_the_value() => ((IDictionary<string, object?>)Mapped)["Street"].ShouldEqual("Main street");
}
