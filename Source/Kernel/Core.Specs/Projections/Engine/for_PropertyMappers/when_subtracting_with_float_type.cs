// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_subtracting_with_float_type : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    AppendedEvent _event;
    ExpandoObject _result;

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

        dynamic target = _result;
        target.property = 42f;
        _propertyMapper = PropertyMappers.SubtractWithEventValueProvider(new TypeFormats(), "property", new JsonSchemaProperty { Type = JsonObjectType.Number, Format = "float" }, _ => 10f);
    }

    void Because() => _propertyMapper(_event, _result, ArrayIndexers.NoIndexers);

    [Fact] void should_convert_result_to_float() => ((object)((dynamic)_result).property).ShouldBeOfExactType<float>();
    [Fact] void should_result_in_expected_value() => ((float)((dynamic)_result).property).ShouldEqual(32f);
}
