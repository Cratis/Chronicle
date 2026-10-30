// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_receiving_events;

public class and_the_root_is_uninitialized : given.a_pinned_subscriber
{
    ObserverSubscriberResult _result;
    ExpandoObject _state;

    void Establish()
    {
        _event = _event with { Context = _event.Context with { EventType = _pin } };
        _state = new ExpandoObject();
        var values = (IDictionary<string, object?>)_state;
        values[WellKnownProperties.ReadModelInstanceInitialized] = false;
        values["name"] = "Joined before creation";
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.HasChanges.Returns(true);
        changeset.CurrentState.Returns(_state);
        _cachedPipeline.Handle(_event).Returns(new ProjectionEventContext("the-partition", _event, changeset, ProjectionOperationType.Join, false));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_keep_handling_the_event() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_advance_the_observed_position() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
    [Fact] void should_not_publish_the_placeholder() => _notifier.DidNotReceive().Notify(_key.Namespace, "the-partition", Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
    [Fact] void should_preserve_the_internal_state() => ((IDictionary<string, object?>)_state)["name"].ShouldEqual("Joined before creation");
}
