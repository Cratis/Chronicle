// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_repairable_indexes : an_event_sequence
{
    protected Storage.InMemory.EventSequences.EventSequenceStorage _log;
    protected Storage.InMemory.Events.Constraints.UniqueConstraintsStorage _claims;
    protected Storage.InMemory.Events.Constraints.ClosedStreamsConstraintStorage _closures;
    protected UniqueConstraintDefinition _uniqueDefinition;
    protected ClosesStreamConstraintDefinition _closingDefinition;
    protected SnapshottingSequenceStorage _snapshots;
    protected IUniqueConstraintsStorage _unique;
    protected IClosedStreamsConstraintStorage _closed;
    protected UniqueConstraintDefinition _otherDefinition;
    protected bool _failWrites;
    protected bool _failReads;
    protected int _rangeReads;
    protected int _claimWrites;
    protected AppendResult _result;
    protected virtual EventSequenceNumber? Watermark => EventSequenceNumber.First;
    protected virtual EventSequenceNumber InitialNext => EventSequenceNumber.First;
    protected virtual bool Closing => false;
    protected virtual bool IncludeUniqueWithClosing => false;
    protected virtual UniqueConstraintMode UniqueMode => UniqueConstraintMode.PerEventSource;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _silo = new();
        ConfigureSilo();
        _snapshots = new(state => _log.SaveState(state));
        _silo.Options.StorageFactory = _ => _snapshots;
        _stateStorage = _silo.StorageManager.GetStorage<EventSequenceState>(typeof(EventSequence).FullName!);
        _stateStorage.State.ConstraintIndexWatermark = Watermark;
        _stateStorage.State.SequenceNumber = InitialNext;
        _claims = new();
        _closures = new();
        _log = new(EventStore, EventStoreNamespace, SequenceId, _identityStorage);
        _uniqueDefinition = new("unique", [new(_eventType.Id, ["email"])], ["Removed"]) { Mode = UniqueMode };
        _closingDefinition = new("closing", [_eventType.Id], ClosedStreamDimensions.EventSourceId, ["Reopened"]);
        var unique = _unique = Substitute.For<IUniqueConstraintsStorage>();
        unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _claimWrites++;
                return _failWrites && call.ArgAt<UniqueConstraintDefinition>(1).Name == _uniqueDefinition.Name ? Task.FromException(new SimulatedStateWriteError()) : _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4), call.ArgAt<CancellationToken>(5));
            });
        unique.GetFor(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.GetFor(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2), call.ArgAt<CancellationToken>(3)));
        unique.GetForStream(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.GetForStream(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2), call.ArgAt<CancellationToken>(3)));
        unique.RemoveValue(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.RemoveValue(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<UniqueConstraintValue>(2), call.ArgAt<string>(3), call.ArgAt<CancellationToken>(4)));
        unique.IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.IsAllowed(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<UniqueConstraintValue>(2), call.ArgAt<string>(3), call.ArgAt<CancellationToken>(4)));
        unique.Remove(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.Remove(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2), call.ArgAt<CancellationToken>(3)));
        var closed = _closed = Substitute.For<IClosedStreamsConstraintStorage>();
        closed.Reopen(Arg.Any<ClosedStreamOwner>(), Arg.Any<ClosedStreamScope>(), Arg.Any<CancellationToken>())
            .Returns(call => _failWrites ? Task.FromException(new SimulatedStateWriteError()) : _closures.Reopen(call.ArgAt<ClosedStreamOwner>(0), call.ArgAt<ClosedStreamScope>(1), call.ArgAt<CancellationToken>(2)));
        closed.Close(Arg.Any<ClosedStream>(), Arg.Any<CancellationToken>())
            .Returns(call => _failWrites ? Task.FromException(new SimulatedStateWriteError()) : _closures.Close(call.ArgAt<ClosedStream>(0), call.ArgAt<CancellationToken>(1)));
        closed.GetFor(Arg.Any<ClosedStreamOwner>(), Arg.Any<ClosedStreamScope>(), Arg.Any<CancellationToken>())
            .Returns(call => _closures.GetFor(call.ArgAt<ClosedStreamOwner>(0), call.ArgAt<ClosedStreamScope>(1), call.ArgAt<CancellationToken>(2)));
        closed.GetCovering(Arg.Any<ClosedStreamScope>(), Arg.Any<IEnumerable<ClosedStreamDimensions>>())
            .Returns(call => _closures.GetCovering(call.ArgAt<ClosedStreamScope>(0), call.ArgAt<IEnumerable<ClosedStreamDimensions>>(1)));
        _namespaceStorage.GetUniqueConstraintsStorage(SequenceId).Returns(unique);
        _namespaceStorage.GetClosedStreamsConstraints(SequenceId).Returns(closed);
        var otherDefinition = _otherDefinition = new UniqueConstraintDefinition("other", [new("OtherEvent", ["email"])]);
        _currentValidation = Closing
            ? new ConstraintValidation([new ClosesStreamConstraintValidator(_closingDefinition, closed), new ClosedStreamConstraintValidator(closed, [_closingDefinition.Dimensions], [_closingDefinition])])
            : new ConstraintValidation([new UniqueConstraintValidator(_uniqueDefinition, unique), new UniqueConstraintValidator(otherDefinition, unique)]);
        _registeredConstraints.Add(Closing ? _closingDefinition : _uniqueDefinition);
        if (!Closing) _registeredConstraints.Add(otherDefinition);
        if (IncludeUniqueWithClosing)
        {
            _registeredConstraints.Add(_uniqueDefinition);
            _currentValidation = new ConstraintValidation([new UniqueConstraintValidator(_uniqueDefinition, unique), new ClosesStreamConstraintValidator(_closingDefinition, closed), new ClosedStreamConstraintValidator(closed, [_closingDefinition.Dimensions], [_closingDefinition])]);
        }
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _rangeReads++;
                if (_failReads) throw new SimulatedStateWriteError();
                return new EventCursor(_log.Events.Where(e => e.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0) && call.ArgAt<IEnumerable<EventType>>(5).Any(t => t.Id == e.Context.EventType.Id)).ToList());
            });
        _expandoObjectConverter.ToExpandoObject(Arg.Any<System.Text.Json.Nodes.JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => Content());
        _eventSequenceStorage.Append(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<IEnumerable<Concepts.Identities.IdentityId>>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<DateTimeOffset>(), Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(), Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(), Arg.Any<Subject?>())
            .Returns(call => _log.Append(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSourceType>(1), call.ArgAt<EventSourceId>(2), call.ArgAt<EventStreamType>(3), call.ArgAt<EventStreamId>(4), call.ArgAt<EventType>(5), call.ArgAt<CorrelationId>(6), call.ArgAt<IEnumerable<Concepts.Auditing.Causation>>(7), call.ArgAt<IEnumerable<Concepts.Identities.IdentityId>>(8), call.ArgAt<IEnumerable<Tag>>(9), call.ArgAt<DateTimeOffset>(10), call.ArgAt<IDictionary<EventTypeGeneration, ExpandoObject>>(11), call.ArgAt<IDictionary<EventTypeGeneration, EventHash>>(12), call.ArgAt<Subject?>(13)));
        return await base.CreateEventSequence();
    }

    protected static ExpandoObject Content(string email = "claimed@example.com")
    {
        dynamic content = new ExpandoObject();
        content.email = email;
        return content;
    }

    protected async Task Reactivate()
    {
        await _eventSequence.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);
        _silo = new();
        ConfigureSilo();
        _snapshots = new(_log.SaveState);
        _silo.Options.StorageFactory = _ => _snapshots;
        _stateStorage = _silo.StorageManager.GetStorage<EventSequenceState>(typeof(EventSequence).FullName!);
        _stateStorage.State = await _log.GetState();
        _eventSequence = await _silo.CreateGrainAsync<EventSequence>(_eventSequenceKey.ToString());
    }

    protected async Task Seed(EventSequenceNumber number, EventSourceId? source = null, EventType? type = null, string email = "claimed@example.com") => await _log.Append(number, EventSourceType.Default, source ?? _eventSourceId, EventStreamType.All, EventStreamId.Default, type ?? _eventType, CorrelationId.NotSet, [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [(type ?? _eventType).Generation] = Content(email) }, new Dictionary<EventTypeGeneration, EventHash>());
}
