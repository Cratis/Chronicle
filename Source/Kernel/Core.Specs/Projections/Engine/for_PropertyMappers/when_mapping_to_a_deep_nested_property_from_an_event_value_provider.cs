// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_mapping_to_a_deep_nested_property_from_an_event_value_provider : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    AppendedEvent _event;
    ExpandoObject _result;
    AppendedEvent _providedEvent;

    void Establish()
    {
        _result = new();
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

        _propertyMapper = PropertyMappers.FromEventValueProvider("deep.nested.property", _ =>
        {
            _providedEvent = _;
            return 42;
        });
    }

    void Because() => _propertyMapper(_event, _result, ArrayIndexers.NoIndexers);

    [Fact] void should_set_value_in_expected_property() => ((object)((dynamic)_result).deep.nested.property).ShouldEqual(42);
    [Fact] void should_pass_the_event_to_the_value_provider() => _providedEvent.ShouldEqual(_event);
}
