// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_inner_subscription_is_disposed : Specification
{
    Subject<ProjectionEventContext> _subject;
    IEventSequenceStorage _eventSequenceStorage;
    bool _itemReceived;
    IDisposable _subscription;

    void Establish()
    {
        _subject = new Subject<ProjectionEventContext>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventSequenceStorage
            .TryGetLastEventBefore(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(Option<AppendedEvent>.None())));

        _subscription = _subject
            .ResolveJoin(_eventSequenceStorage, new EventType(new EventTypeId("e1"), 1), PropertyPath.Root, Substitute.For<ILogger>())
            .Subscribe(_ => _itemReceived = true);
    }

    void Because()
    {
        _subscription.Dispose();

        var @event = new AppendedEvent(
            new(
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                1,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new ExpandoObject());

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.CurrentState.Returns(new ExpandoObject());
        changeset.Incoming.Returns(@event);

        _subject.OnNext(new(
            new(@event.Context.EventSourceId, ArrayIndexers.NoIndexers),
            @event,
            changeset,
            ProjectionOperationType.From,
            false));
    }

    [Fact] void should_not_receive_items_after_disposal() => _itemReceived.ShouldBeFalse();
}
