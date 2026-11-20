// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.EventSequences;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// An event sequence whose state write hangs, with a shared stop deadline the spec cancels.
/// </summary>
public class an_event_sequence_with_a_hanging_state_write : an_event_sequence
{
    protected readonly CancellationTokenSource _deadline = new();

    protected override async Task<EventSequence> CreateEventSequence()
    {
        var budget = Substitute.For<IShutdownDrainBudget>();
        budget.Token.Returns(_deadline.Token);
        _silo.AddService(budget);
        _stateStorage = _silo.StorageManager.GetStorage<EventSequenceState>(typeof(EventSequenceWhoseStateWriteNeverCompletes).FullName!);
        _stateStorage.State.ConstraintIndexWatermark = EventSequenceNumber.First;
        return await _silo.CreateGrainAsync<EventSequenceWhoseStateWriteNeverCompletes>(_eventSequenceKey.ToString());
    }

    void Destroy() => _deadline.Dispose();
}
