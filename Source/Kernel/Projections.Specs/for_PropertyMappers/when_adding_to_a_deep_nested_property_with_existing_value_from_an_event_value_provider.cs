// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.for_PropertyMappers;

public class when_adding_to_a_deep_nested_property_with_existing_value_from_an_event_value_provider : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    AppendedEvent _event;
    ExpandoObject _result;
    AppendedEvent _providedEvent;

    void Establish()
    {
        dynamic content = new ExpandoObject();
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
                []),
            new ExpandoObject());

        dynamic target = _result;
        target.deep = new ExpandoObject();
        target.deep.nested = new ExpandoObject();
        target.deep.nested.property = 42;
        _propertyMapper = PropertyMappers.AddWithEventValueProvider(new TypeFormats(), "deep.nested.property", new JsonSchemaProperty { Type = JsonObjectType.Integer }, _ =>
        {
            _providedEvent = _;
            return 42;
        });
    }

    void Because() => _propertyMapper(_event, _result, ArrayIndexers.NoIndexers);

    [Fact] void should_result_in_expected_value() => ((object)((dynamic)_result).deep.nested.property).ShouldEqual(84);
    [Fact] void should_pass_the_event_to_the_value_provider() => _providedEvent.ShouldEqual(_event);
}
