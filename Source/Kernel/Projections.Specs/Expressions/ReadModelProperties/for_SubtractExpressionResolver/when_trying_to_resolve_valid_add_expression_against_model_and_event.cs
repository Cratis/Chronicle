// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ReadModelProperties.for_SubtractExpressionResolver;

public class when_trying_to_resolve_valid_add_expression_against_model_and_event : Specification
{
    IEventValueProviderExpressionResolvers _eventValueResolvers;
    SubtractExpressionResolver _resolver;
    AppendedEvent _event;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        dynamic targetAsDynamic = _target;
        targetAsDynamic.targetProperty = 42d;
        var content = new ExpandoObject();
        ((dynamic)content).sourceProperty = 2d;
        _event = new(
            new(0,
            new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)),
            new(
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
                Identity.System),
            content);

        _eventValueResolvers = Substitute.For<IEventValueProviderExpressionResolvers>();
        _eventValueResolvers.Resolve(Arg.Any<JsonSchemaProperty>(), Arg.Any<string>()).Returns(_ => 2d);
        _resolver = new(_eventValueResolvers);
    }

    void Because() => _resolver.Resolve("targetProperty", new(), "$subtract(sourceProperty)")(_event, _target, ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_to_a_propertymapper_that_can_subtract_from_the_property() => ((double)((dynamic)_target).targetProperty).ShouldEqual(40d);
}
