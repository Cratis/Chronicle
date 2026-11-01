// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_catching_up_sequence_with_a_failing_constraint : a_catching_up_sequence
{
    protected bool _otherFails;
    protected int _otherSaves;
    protected readonly object[] _owners = [new(), new(), new()];
    protected readonly object _waiting = new();

    void Establish()
    {
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var definition = call.ArgAt<UniqueConstraintDefinition>(1);
                if (definition.Name == _otherDefinition.Name)
                {
                    _otherSaves++;
                    if (_otherFails) return Task.FromException(new SimulatedStateWriteError());
                }
                if (definition.Name == _uniqueDefinition.Name && _failWrites) return Task.FromException(new SimulatedStateWriteError());
                _claimWrites++;
                return _claims.Save(call.ArgAt<EventSourceId>(0), definition, call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4));
            });
        _clock.TimerCreated = due =>
        {
            if (due.TotalMilliseconds is 100 or 400) _clock.Advance(due);
        };
    }

    protected void OccupySlots()
    {
        var slots = ConstraintIndexCatchUpSlots.For(_storage, ConstraintIndexRepairConfiguration.CatchUpSlots);
        foreach (var owner in _owners) slots.TryEnter(owner).ShouldBeTrue();
    }

    protected bool WaitingAdmitted() => ConstraintIndexCatchUpSlots.For(_storage, ConstraintIndexRepairConfiguration.CatchUpSlots).TryEnter(_waiting);

    protected async Task AppendOther(EventSourceId source)
    {
        var eventType = _eventType;
        var eventSourceId = _eventSourceId;
        _eventType = new("OtherEvent", EventTypeGeneration.First);
        _eventSourceId = source;
        try
        {
            await AppendAnEvent();
        }
        finally
        {
            _eventType = eventType;
            _eventSourceId = eventSourceId;
        }
    }

    protected async Task<UniqueConstraintValue[]> OtherValues(string source) => (await _claims.GetFor(source, _otherDefinition)).ToArray();

    void Destroy()
    {
        var slots = ConstraintIndexCatchUpSlots.For(_storage, ConstraintIndexRepairConfiguration.CatchUpSlots);
        foreach (var owner in _owners) slots.Complete(owner);
        slots.Complete(_waiting);
    }
}
