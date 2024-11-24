// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ModelProperties.for_SubtractExpressionResolver;

public class when_trying_to_resolve_valid_add_expression_against_model_and_event : Specification
{
    Mock<IEventValueProviderExpressionResolvers> event_value_resolvers;
    SubtractExpressionResolver resolver;
    AppendedEvent @event;
    ExpandoObject target;

    void Establish()
    {
        target = new();
        dynamic targetAsDynamic = target;
        targetAsDynamic.targetProperty = 42d;
        var content = new ExpandoObject();
        ((dynamic)content).sourceProperty = 2d;
        @event = new(
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

        event_value_resolvers = new();
        event_value_resolvers.Setup(_ => _.Resolve(Arg.Any<JsonSchemaProperty>(), Arg.Any<string>())).Returns((AppendedEvent _) => 2d);
        resolver = new(event_value_resolvers.Object);
    }

    void Because() => resolver.Resolve("targetProperty", new(), "$subtract(sourceProperty)")(@event, target, ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_to_a_propertymapper_that_can_subtract_from_the_property() => ((double)((dynamic)target).targetProperty).ShouldEqual(40d);
}
