// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.given;

public class a_page_subscriber : a_subscriber_with_a_cached_pipeline
{
    protected AppendedEvent[] _events;
    protected readonly List<EventSequenceNumber> _handled = [];

    void Establish()
    {
        _events = new[] { "parent", "child", "parent" }.Select((partition, index) =>
            AppendedEvent.EmptyWithEventSequenceNumber((ulong)index + 1) with
            {
                Context = EventContext.Empty with { SequenceNumber = (ulong)index + 1, EventSourceId = partition }
            }).ToArray();
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject());
        _cachedPipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call =>
        {
            var @event = call.Arg<AppendedEvent>();
            _handled.Add(@event.Context.SequenceNumber);
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.HasChanges.Returns(true);
            changeset.CurrentState.Returns(new ExpandoObject());
            return new ProjectionEventContext(@event.Context.EventSourceId, @event, changeset, ProjectionOperationType.From, false);
        });
    }
}
