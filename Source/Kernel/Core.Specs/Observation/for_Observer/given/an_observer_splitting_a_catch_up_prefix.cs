// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_splitting_a_catch_up_prefix : an_observer_with_a_handover_log
{
    protected AppendedEvent _liveEvent;
    protected EventSequenceNumber _accepted;
    protected virtual bool HoldT => true;
    protected virtual bool IncludeApplicableEventAtFourteen => false;

    protected async Task SetUpPrefix()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        Append("A", 10UL);
        var heldPrefix = Append("P", 11UL);
        Append("A", 13UL);
        var atFourteen = IncludeApplicableEventAtFourteen ? Append("P", 14UL) : Append("X", 14UL, EventType.Unknown);
        var heldSuffix = HoldT ? Append("T", 15UL) : null;
        _liveEvent = Append("Q", 16UL);
        await _observer.RegisterCatchingUpPartitions(HoldT ? ["P", "T"] : ["P"]);
        await _observer.Handle("P", [heldPrefix]);
        if (IncludeApplicableEventAtFourteen) await _observer.Handle("P", [atFourteen]);
        if (heldSuffix is not null) await _observer.Handle("T", [heldSuffix]);
    }

    protected async Task Join()
    {
        _accepted = PersistedObserverState.NextEventSequenceNumber;
        Append("Q", 17UL);
        await _silo.RunCatchUpContinuation();
    }
}
