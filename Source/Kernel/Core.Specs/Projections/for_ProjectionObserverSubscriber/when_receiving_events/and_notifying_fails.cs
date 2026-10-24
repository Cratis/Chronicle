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

public class and_notifying_fails : given.a_pinned_subscriber
{
    ObserverSubscriberResult _result;

    void Establish()
    {
        _event = _event with { Context = _event.Context with { EventType = _pin } };
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.HasChanges.Returns(true);
        changeset.CurrentState.Returns(new ExpandoObject());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject());
        _cachedPipeline.Handle(_event).Returns(new ProjectionEventContext("the-partition", _event, changeset, ProjectionOperationType.From, false));
        _notifier.Notify(_key.Namespace, "the-partition", Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_return_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_keep_the_handled_position() => _result.LastSuccessfulObservation.ShouldEqual(_event.Context.SequenceNumber);
    [Fact] void should_carry_the_exception_message() => _result.ExceptionMessages.ShouldContain("boom");
}
