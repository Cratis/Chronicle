// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionBuilder.when_building;

public class with_a_composite_key : Specification
{
    static readonly EventType _eventType = new("some-event", 1);

    KernelProjectionBuilder<a_read_model> _builder;
    ProjectionDefinition _result;

    void Establish()
    {
        _builder = new(WellKnownKernelProjections.IdentifierFor("event-type-distribution"), "event-type-distribution");
        _builder
            .IdentifiedByComposite(key => key
                .With(model => model.EventType, $"{WellKnownExpressions.EventContext}.eventType")
                .With(model => model.Namespace, $"{WellKnownExpressions.EventContext}.@namespace"))
            .From(_eventType, from => from.Count(model => model.Count));
    }

    void Because() => _result = _builder.Build();

    [Fact] void should_build_a_composite_key_expression() =>
        _result.From[_eventType].Key.Value.ShouldEqual("$composite(EventType=$eventContext.eventType,Namespace=$eventContext.@namespace)");

    [Fact] void should_count_into_the_count_property() =>
        _result.From[_eventType].Properties[new("Count")].ShouldEqual(WellKnownExpressions.Count);
}
