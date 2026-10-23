// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_events_are_replayed : given.a_pinned_subscriber
{
    ObserverSubscriberResult _result;

    void Establish()
    {
        _event = _event with { Context = _event.Context with { EventType = _pin, ObservationState = EventObservationState.Replay } };
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.HasChanges.Returns(true);
        changeset.CurrentState.Returns(new ExpandoObject());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject());
        _cachedPipeline.Handle(_event).Returns(new ProjectionEventContext("the-partition", _event, changeset, ProjectionOperationType.From, false));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_handle_the_replay_event_successfully() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_keep_notifying_connected_watchers() => _notifier.Received(1).Notify(_key.Namespace, "the-partition", Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
