// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.AspNetCore.DataProtection;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor.given;

public class an_administrators_reactor : Specification
{
    protected IStorage _storage;
    protected IEventSequenceStorage _events;
    protected AdministratorsReactor _reactor;
    protected UserId _userId;
    protected UserGrantedAdministrator _grant;
    protected IKernelEventProof _proof;
    protected readonly List<AppendedEvent> _appended = [];
    protected readonly Dictionary<AppendedEvent, object> _contents = [];

    protected EventContext SeedGrant() => Seed(Roster.EventSourceId, _grant);

    protected EventContext Seed(EventSourceId source, object content, EventStreamType? streamType = null, EventStreamId? streamId = null) =>
        Append(source, SignedContent(source, content, _proof), streamType, streamId);

    protected EventContext SeedWithDifferentProof(EventSourceId source, object content, IKernelEventProof signer) =>
        Append(source, SignedContent(source, content, signer), null, null);

    protected EventContext SeedWithoutProof(EventSourceId source, object content, EventStreamId streamId) =>
        Append(source, content, null, streamId);

    object SignedContent(EventSourceId source, object content, IKernelEventProof signer)
    {
        var predecessor = _appended.LastOrDefault(@event => @event.Context.EventSourceId == source)?.Context.SequenceNumber ?? EventSequenceNumber.BeforeFirst;
        return RosterHistory.WithProof(content, source, predecessor, signer);
    }

    EventContext Append(EventSourceId source, object content, EventStreamType? streamType, EventStreamId? streamId)
    {
        var appended = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventStore = EventStoreName.System,
                Namespace = EventStoreNamespaceName.Default,
                EventSourceId = source,
                EventStreamType = streamType ?? EventStreamType.All,
                EventStreamId = streamId ?? EventStreamId.Default,
                EventType = content.GetType().GetEventType(),
                SequenceNumber = (ulong)_appended.Count
            }
        };
        _appended.Add(appended);
        _contents.Add(appended, content);
        return appended.Context;
    }

    protected void RedactSlot(EventSequenceNumber sequence)
    {
        var slot = _appended.Single(@event => @event.Context.SequenceNumber == sequence);
        var tombstone = slot with { Context = slot.Context with { EventType = new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First) } };
        _appended[_appended.IndexOf(slot)] = tombstone;
        _contents[tombstone] = new EventRedacted("spec", slot.Context.EventType.Id, DateTimeOffset.UtcNow, slot.Context.CorrelationId, [], []);
    }

    static IEventCursor Cursor(AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(events);
        cursor.MoveNext().Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }

    void Establish()
    {
        _proof = new KernelEventProof(new EphemeralDataProtectionProvider());
        _userId = Guid.NewGuid();
        _grant = new UserGrantedAdministrator(_userId, GrantReason.Bootstrap, "spec");
        _storage = Substitute.For<IStorage>();
        _storage.System.Returns(new Storage.InMemory.SystemStorage());
        _events = Substitute.For<IEventSequenceStorage>();
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log).Returns(_events);
        var serializer = Substitute.For<IEventSerializer>();
        serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(call => _contents[call.Arg<AppendedEvent>()]);
        _events.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>())
            .Returns(call => Task.FromResult(Matching(call.ArgAt<IEnumerable<EventType>>(0), call.ArgAt<EventSourceId>(1)).LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable));
        _events.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Cursor(Matching(call.ArgAt<IEnumerable<EventType>>(3), call.ArgAt<EventSourceId>(2))
                .Where(@event => @event.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0) && @event.Context.SequenceNumber <= call.ArgAt<EventSequenceNumber>(1)).ToArray())));
        _reactor = new AdministratorsReactor(_storage, serializer, _proof);
    }

    IEnumerable<AppendedEvent> Matching(IEnumerable<EventType>? types, EventSourceId? source) =>
        _appended.Where(@event => types?.Contains(@event.Context.EventType) != false && (source is null || source == @event.Context.EventSourceId));
}
