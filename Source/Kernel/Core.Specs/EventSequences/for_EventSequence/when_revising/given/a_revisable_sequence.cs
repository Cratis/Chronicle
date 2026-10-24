// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class a_revisable_sequence : for_EventSequence.given.an_event_sequence_with_repairable_indexes
{
    protected EventSequenceNumber? _watermarkWhenStorageRevised;
    protected bool _storageRevised;
    protected bool _failRevise;
    protected Exception? _error;
    protected int _stateWritesBeforeRevise;
    protected override EventSequenceNumber InitialNext => 1;
    protected override EventSequenceNumber? Watermark => 1;

    protected UniqueConstraintValue ValueOf(string email) =>
        new(_uniqueDefinition.GetPropertiesAndValues(new ConstraintValidationContext([], _eventSourceId, _eventType.Id, Content(email))).GetValue(_uniqueDefinition.IgnoreCasing));

    async Task Establish()
    {
        _eventSequenceStorage.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => _log.GetEventAt(call.ArgAt<EventSequenceNumber>(0)));
        _eventSequenceStorage.Revise(Arg.Any<EventSequenceNumber>(), Arg.Any<EventType>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<IEnumerable<IdentityId>>(), Arg.Any<DateTimeOffset>(), Arg.Any<ExpandoObject>(), Arg.Any<EventHash>())
            .Returns(call =>
            {
                _storageRevised = true;
                _watermarkWhenStorageRevised = _snapshots.Written?.ConstraintIndexWatermark;
                return _failRevise
                    ? Task.FromException(new for_EventSequence.given.SimulatedStateWriteError())
                    : _log.Revise(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventType>(1), call.ArgAt<CorrelationId>(2), call.ArgAt<IEnumerable<Causation>>(3), call.ArgAt<IEnumerable<IdentityId>>(4), call.ArgAt<DateTimeOffset>(5), call.ArgAt<ExpandoObject>(6), call.ArgAt<EventHash>(7));
            });
        await Seed(0);
        _stateWritesBeforeRevise = _snapshots.Snapshots.Count;
    }

    protected async Task ReviseFirstEvent() => await ReviseAt(0, _eventType);

    protected async Task ReviseAt(EventSequenceNumber position, EventType type) =>
        _error = await Catch.Exception(() => _eventSequence.Revise(position, type, new JsonObject(), CorrelationId.New(), [], Identity.System));
}
