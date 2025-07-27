// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Expressions.ReadModelProperties.for_DecrementExpressionResolver;

public class when_trying_to_resolve_valid_decrement_expression_against_model_with_no_content : Specification
{
    DecrementExpressionResolver _resolver;
    AppendedEvent _event;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        var content = new ExpandoObject();
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
        _resolver = new(new TypeFormats());
    }

    void Because()
    {
        _resolver.Resolve("targetProperty", new(), "$decrement()")(_event, _target, ArrayIndexers.NoIndexers);
        _resolver.Resolve("targetProperty", new(), "$decrement()")(_event, _target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_resolve_to_a_propertymapper_that_counts_decrements_the_property() => ((double)((dynamic)_target).targetProperty).ShouldEqual(-2d);
}
