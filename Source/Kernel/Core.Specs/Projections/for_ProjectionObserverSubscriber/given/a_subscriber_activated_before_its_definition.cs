// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.given;

public class a_subscriber_activated_before_its_definition : a_subscriber_with_a_cached_pipeline
{
    protected AppendedEvent _event;

    protected override ProjectionDefinition DefinitionAtActivation() => _definition with { ReadModel = null! };

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(new("new-event", 1));
        _event = _event with { Context = _event.Context with { SequenceNumber = 42 } };
        _cachedPipeline.Handle(_event).Returns(ProjectionEventContext.Empty(Substitute.For<Changes.IObjectComparer>(), _event));
        _projectionGrain.ClearReceivedCalls();
    }
}
