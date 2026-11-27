// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_splitting_a_catch_up_prefix : an_observer_with_a_handover_log
{
    protected AppendedEvent _liveEvent;
    protected ObserverState _accepted;
    protected CatchUpObserverPartitionRequest[] _acceptedRepairs;
    protected virtual bool IncludeApplicableEventAtFourteen => false;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        Append("A", 10UL);
        var heldPrefix = Append("P", 11UL);
        Append("A", 13UL);
        var atFourteen = IncludeApplicableEventAtFourteen ? Append("P", 14UL) : Append("X", 14UL, EventType.Unknown);
        var heldSuffix = Append("T", 15UL);
        _liveEvent = Append("Q", 16UL);
        await _observer.RegisterCatchingUpPartitions(["P", "T"]);
        await _observer.Handle("P", [heldPrefix]);
        if (IncludeApplicableEventAtFourteen) await _observer.Handle("P", [atFourteen]);
        await _observer.Handle("T", [heldSuffix]);
    }

    protected async Task DrainRepairsAndRunWideReader()
    {
        _accepted = PersistedObserverState;
        Append("Q", 17UL);
        await _silo.RunCatchUpContinuation();
        _acceptedRepairs = [.. _repairs];
        foreach (var repair in _acceptedRepairs) await FinishRepair(repair);
        if (_acceptedRepairs.Length > 0) await _silo.RunCatchUpContinuation();
        if (_wide is { } wide) await DeliverRange(wide.FromEventSequenceNumber, EventSequenceNumber.Max, wide.EventTypes);
    }
}
