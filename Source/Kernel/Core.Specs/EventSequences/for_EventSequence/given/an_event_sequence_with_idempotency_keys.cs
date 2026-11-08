// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences.Idempotency;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using NSubstitute.ExceptionExtensions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_idempotency_keys : appending_many_events
{
    protected readonly IdempotencyKey _key = new("one");
    protected IEventPublicationStorage _publications;
    protected IEncryptionKeyStorage _keys;
    protected EventToAppend _request;
    protected IdempotentAppendResult _result;
    protected ConcurrencyScope _scope = ConcurrencyScope.None;
    protected List<EventToAppendToStorage> _written = [];
    protected Dictionary<string, StoredEventPublication> _durable = [];
    protected int _lookups;
    protected int _writes;
    protected InMemoryEncryptionKeyStorage _backingKeys = new();
    protected Storage.InMemory.EventSequences.EventSequenceStorage _log;
    protected EncryptionKey _protectingKey = new([1, 2, 3], [4, 5, 6]);

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage, IEventPublicationStorage>();
        _publications = (IEventPublicationStorage)_eventSequenceStorage;
        _keys = Substitute.For<IEncryptionKeyStorage>();
        _silo.AddService(_keys);
        _namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        _publications.TryGetPublications(Arg.Any<IEnumerable<string>>()).Returns(_ =>
        {
            _lookups++;
            return new Dictionary<string, StoredEventPublication>(_durable);
        });
        _log = new(EventStore, EventStoreNamespace, SequenceId, _identityStorage);
        _eventSequenceStorage.GetTailSequenceNumber().Returns(_ => _log.GetTailSequenceNumber());
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => _log.GetRange(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), cancellationToken: call.ArgAt<CancellationToken>(5)));
        _eventSequenceStorage.ReadBackAppend(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>())
            .Returns(call => _log.ReadBackAppend(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<CancellationToken>(2)));
        _eventHashCalculator.Calculate(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<ExpandoObject>()).Returns((EventHash)"submitted-hash");
        _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
        {
            _writes++;
            _written = call.Arg<IEnumerable<EventToAppendToStorage>>().ToList();
            return Task.FromResult<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>>(Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(AppendedEventsFrom(_written)));
        });
        _complianceManager.LocateProtectedValues(Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(_ => []);
        _complianceManager.ApplyReportingKeys(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => (call.Arg<JsonObject>().DeepClone().AsObject(), (IReadOnlyList<ProtectedValueUse>)[]));
        return await base.CreateEventSequence();
    }

    void Establish() => _request = EventToAppendFor(_eventSourceId);

    protected Task<IdempotentAppendResult> AppendKeyed() => _eventSequence.AppendWithIdempotencyKey(_request, _key, CorrelationId.New(), [], Identity.System, _scope);

    internal AppendFingerprintInput Input(EventToAppend request, IReadOnlyList<ProtectedValueLocation>? locations = null) => new(
        request.EventType,
        request.Content,
        request.EventSourceId,
        request.EventSourceType,
        request.eventStreamType,
        request.eventStreamId,
        request.EventSource,
        request.Subject?.IsSet == true ? request.Subject.Value : request.EventSourceId.Value,
        _key.Identity,
        locations ?? []);

    protected void Durable(EventToAppend? original = null, EventSequenceNumber? number = null, string? fingerprint = null)
    {
        var stored = original ?? _request;
        var encoded = fingerprint ?? AppendFingerprint.Compute(Input(stored), []).AsT0.Format();
        _durable[_key.Identity] = new(number ?? EventSequenceNumber.First, encoded);
        _stateStorage.State.SequenceNumber = (number ?? EventSequenceNumber.First).Next();
    }

    protected void WinnerAppears() => _durable[_key.Identity] = new(EventSequenceNumber.First, AppendFingerprint.Compute(Input(_request), []).AsT0.Format());

    protected async Task Protect(bool erased = false)
    {
        _request.Content["name"] = "Alice";
        var location = new ProtectedValueLocation("name", ProtectionKind.Pii, _eventSourceId.Value, _request.Content["name"]!.DeepClone());
        var use = new ProtectedValueUse("name", ProtectionKind.Pii, location.Plaintext, new(ProtectionKind.Pii, _eventSourceId.Value, _protectingKey, EncryptionKeyRevision.Initial));
        _complianceManager.LocateProtectedValues(Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call =>
            [location with { Plaintext = call.Arg<JsonObject>()["name"]!.DeepClone() }]);
        var encoded = AppendFingerprint.Compute(Input(_request, [location]), [use]).AsT0.Format();
        Durable(fingerprint: encoded);
        await _backingKeys.SaveFor(EventStore, EventStoreNamespace, _eventSourceId.Value, _protectingKey, EncryptionKeyRevision.Initial);
        if (erased)
        {
            await _backingKeys.RecordErasureFor(EventStore, EventStoreNamespace, _eventSourceId.Value);
            await _backingKeys.DeleteFor(EventStore, EventStoreNamespace, _eventSourceId.Value);
        }
        _keys.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKeyRevision?>())
            .Returns(call => _backingKeys.TryGetFor(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2), call.ArgAt<EncryptionKeyRevision?>(3)));
        _keys.GetErasureForRelease(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>())
            .Returns(call => _backingKeys.GetErasureForRelease(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<EncryptionKeyIdentifier>(2)));
    }

    protected async Task SeedSlot(EventSequenceNumber number, EventType? type = null) => await _log.Append(
        number,
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        type ?? _eventType,
        CorrelationId.NotSet,
        [],
        [],
        [],
        DateTimeOffset.UtcNow,
        new Dictionary<EventTypeGeneration, ExpandoObject> { [(type ?? _eventType).Generation] = new() },
        new Dictionary<EventTypeGeneration, EventHash>());

    protected void PendingConstraint()
    {
        _constraintValidation.IndexedDefinitions.Returns([new UniqueConstraintDefinition("pending", [new(_eventType.Id, ["name"])])]);
        _stateStorage.State.SequenceNumber = EventSequenceNumber.First.Next();
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("index is unavailable"));
    }

    protected void RefuseWrites() => _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>())
        .Throws(new EventsNotCommitted(new Exception("proven non-commit")));

    protected async Task ShouldNotWrite() => await _publications.DidNotReceive().AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
