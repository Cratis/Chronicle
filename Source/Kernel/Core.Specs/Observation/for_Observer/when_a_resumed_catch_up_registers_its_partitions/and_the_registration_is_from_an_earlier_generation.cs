// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_resumed_catch_up_registers_its_partitions;

/// <summary>
/// A registration carrying an earlier ownership generation is ignored; the current one is accepted (#5406).
/// </summary>
public class and_the_registration_is_from_an_earlier_generation : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly JobId _jobId = JobId.New();
    bool _isPreparingCatchup;

    bool _heldAfterCurrent;

    async Task Because()
    {
        var earlier = await _observer.ResumingCatchUp(_jobId);
        var current = await _observer.ResumingCatchUp(_jobId);
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, earlier, [(Key)"P"]);
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
        await _observer.RegisterResumedCatchingUpPartitions(_jobId, current, [(Key)"P"]);
        _heldAfterCurrent = _stateStorage.State.CatchingUpPartitions.Contains((Key)"P");
    }

    [Fact] void should_keep_the_blanket_hold_after_the_stale_registration() => _isPreparingCatchup.ShouldBeTrue();
    [Fact] void should_accept_the_current_generation() => _heldAfterCurrent.ShouldBeTrue();
}
