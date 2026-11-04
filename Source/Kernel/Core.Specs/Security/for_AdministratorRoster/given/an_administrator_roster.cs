// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;
using StorageUser = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Security.for_AdministratorRoster.given;

public class an_administrator_roster : Specification
{
    protected IStorage _storage;
    protected IGrainFactory _grainFactory;
    protected IEventSequence _eventLog;
    protected IEventSequenceStorage _events;
    protected IEventSerializer _serializer;
    protected AdministratorRoster _roster;
    protected IKernelEventProof _proof;
    protected ChronicleOptions _options;
    protected List<AppendedEvent> _appended;
    protected Dictionary<JsonObject, object> _serialized;
    protected Dictionary<AppendedEvent, object> _contents;
    protected UserId _first;
    protected UserId _second;
    protected int _appendCalls;
    protected List<ConcurrencyScopes> _scopes = [];
    protected int _conflicts;
    protected bool _failAppend;
    protected bool _synchronizeNextAppends;
    readonly object _appendLock = new();
    readonly TaskCompletionSource<bool> _appendGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _waitingAppends;

    void Establish()
    {
        _proof = new KernelEventProof(new EphemeralDataProtectionProvider());
        _first = Guid.NewGuid();
        _second = Guid.NewGuid();
        _appended = [];
        _serialized = [];
        _contents = [];
        _options = new ChronicleOptions { Authentication = new Authentication { Enabled = true, AdminUser = new() { Username = "root" } } };
        _storage = Substitute.For<IStorage>();
        _storage.System.Returns(new Storage.InMemory.SystemStorage());
        _events = Substitute.For<IEventSequenceStorage>();
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log).Returns(_events);
        _grainFactory = Substitute.For<IGrainFactory>();
        _eventLog = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Serialize(Arg.Any<object>()).Returns(call => Serialize(call[0]));
        _serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(call => Deserialize(call.Arg<AppendedEvent>()));
        ConfigureReads();
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<ConcurrencyScopes>())
            .Returns(call => AppendBatch(call.Arg<IEnumerable<EventToAppend>>().ToArray(), call.Arg<ConcurrencyScopes>()));
        _roster = CreateRoster();
    }

    protected AdministratorRoster CreateRoster() => CreateRosterWith(_proof);

    protected AdministratorRoster CreateRosterWith(IKernelEventProof proof) => new(
        _storage, _grainFactory, _serializer, proof, Options.Create(_options), new ConfigurationBuilder().Build(), NullLogger<AdministratorRoster>.Instance);

    protected async Task AddUser(UserId userId, string username, bool active = true) =>
        await _storage.System.Users.Create(new StorageUser { Id = userId, Username = username, IsActive = active });

    protected AppendedEvent Seed(EventSourceId source, object content) => SeedSigned(source, content);

    protected AppendedEvent SeedSigned(EventSourceId source, object content, IKernelEventProof? signer = null)
    {
        var predecessor = _appended.LastOrDefault(@event => @event.Context.EventSourceId == source)?.Context.SequenceNumber ?? EventSequenceNumber.BeforeFirst;
        return SeedForged(source, RosterHistory.WithProof(content, source, predecessor, signer ?? _proof));
    }

    protected void RedactSlot(EventSequenceNumber sequence)
    {
        var slot = _appended.Single(@event => @event.Context.SequenceNumber == sequence);
        var tombstone = slot with { Context = slot.Context with { EventType = new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First) } };
        _appended[_appended.IndexOf(slot)] = tombstone;
        _contents[tombstone] = new EventRedacted("spec", slot.Context.EventType.Id, DateTimeOffset.UtcNow, slot.Context.CorrelationId, [], []);
    }

    protected EventSequenceNumber SeedRedactedUnrelatedEvent(EventSourceId source)
    {
        var unrelated = SeedForged(source, new ApplicationRemoved());
        RedactSlot(unrelated.Context.SequenceNumber);
        return unrelated.Context.SequenceNumber;
    }

    protected void ReviseSlot(EventSequenceNumber sequence, object content)
    {
        var slot = _appended.Single(@event => @event.Context.SequenceNumber == sequence);
        var revised = slot with { Revisions = [new EventRevision(EventTypeGeneration.First, slot.Context.CorrelationId, [], Identity.System, DateTimeOffset.UtcNow)] };
        _appended[_appended.IndexOf(slot)] = revised;
        _contents[revised] = content;
    }

    protected AppendedEvent SeedForged(EventSourceId source, object content)
    {
        var appended = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventSourceId = source,
                EventType = content.GetType().GetEventType(),
                SequenceNumber = (ulong)_appended.Count
            }
        };
        _appended.Add(appended);
        _contents.Add(appended, content);
        return appended;
    }

    protected void SeedGrant(UserId userId) => Seed(Roster.EventSourceId, new UserGrantedAdministrator(userId, GrantReason.Bootstrap, "spec"));

    protected void ConfigureRecovery(string username, string nonce, bool force = false)
    {
        _options = new ChronicleOptions
        {
            Authentication = new Authentication
            {
                Enabled = true,
                AdminUser = new() { Username = "root" },
                AdministratorRecovery = new() { Username = username, Nonce = nonce, Force = force }
            }
        };
        _roster = CreateRoster();
    }

    protected IEnumerable<T> Recorded<T>() => _appended.Select(@event => _contents[@event]).OfType<T>();

    void ConfigureReads()
    {
        _events.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>())
            .Returns(call => Task.FromResult(Matching(call.ArgAt<IEnumerable<EventType>>(0), call.ArgAt<EventSourceId>(1)).LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable));
        _events.GetNextSequenceNumberGreaterOrEqualThan(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
            .Returns(call => Task.FromResult(Matching(call.ArgAt<IEnumerable<EventType>>(1), call.ArgAt<EventSourceId>(2))
                .FirstOrDefault(@event => @event.Context.SequenceNumber >= call.Arg<EventSequenceNumber>())?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable));
        _events.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => Task.FromResult(_appended.Single(@event => @event.Context.SequenceNumber == call.Arg<EventSequenceNumber>())));
        _events.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Cursor(Matching(call.ArgAt<IEnumerable<EventType>>(3), call.ArgAt<EventSourceId>(2))
                .Where(@event => @event.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0) && @event.Context.SequenceNumber <= call.ArgAt<EventSequenceNumber>(1)).ToArray())));
    }

    IEnumerable<AppendedEvent> Matching(IEnumerable<EventType>? types, EventSourceId? source)
    {
        lock (_appendLock)
        {
            return _appended.Where(@event => types?.Contains(@event.Context.EventType) != false && (source is null || source == @event.Context.EventSourceId)).ToArray();
        }
    }

    object Deserialize(AppendedEvent @event)
    {
        lock (_appendLock) return _contents[@event];
    }

    static IEventCursor Cursor(AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(events);
        cursor.MoveNext().Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }

    JsonObject Serialize(object content)
    {
        lock (_appendLock)
        {
            var json = new JsonObject();
            _serialized.Add(json, content);
            return json;
        }
    }

    async Task<AppendManyResult> AppendBatch(EventToAppend[] events, ConcurrencyScopes scopes)
    {
        if (_synchronizeNextAppends)
        {
            if (Interlocked.Increment(ref _waitingAppends) == 2) _appendGate.TrySetResult(true);
            await _appendGate.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        lock (_appendLock)
        {
            _appendCalls++;
            _scopes.Add(scopes);
            if (_failAppend) return new AppendManyResult { CorrelationId = CorrelationId.New(), Errors = ["storage failure"] };
            var violations = scopes.Scopes.Where(entry => Violates(entry.Key, entry.Value))
                .Select(entry => new ConcurrencyViolation(entry.Key, entry.Value.SequenceNumber, Matching(entry.Value.EventTypes, entry.Key).Last().Context.SequenceNumber)).ToArray();
            if (violations.Length != 0)
            {
                _conflicts++;
                return AppendManyResult.Failed(CorrelationId.New(), violations);
            }
            var sequenceNumbers = events.Select(@event => SeedForged(@event.EventSourceId, _serialized[@event.Content]).Context.SequenceNumber).ToArray();
            return new AppendManyResult { CorrelationId = CorrelationId.New(), SequenceNumbers = sequenceNumbers, ConcurrencyCheckPerformed = true };
        }
    }

    bool Violates(EventSourceId source, ConcurrencyScope scope)
    {
        var tail = Matching(scope.EventTypes, source).LastOrDefault()?.Context.SequenceNumber;
        return tail is not null && (scope.ExpectsNoMatchingEvent || tail > scope.SequenceNumber);
    }
}
