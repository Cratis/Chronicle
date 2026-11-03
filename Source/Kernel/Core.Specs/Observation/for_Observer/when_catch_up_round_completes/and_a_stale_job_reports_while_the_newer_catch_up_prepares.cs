// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// A catch-up job whose claim has passed to a newer catch-up concludes nothing: the preparation, cursor and claim all
/// belong to the newer job, so live delivery must not resume beside it.
/// </summary>
public class and_a_stale_job_reports_while_the_newer_catch_up_prepares : given.a_newer_catch_up_started_after_a_stale_one
{
    bool _preparing;

    Task Establish() => StartNewerCatchUp();

    async Task Because()
    {
        await ReportStaleCatchUp();
        _preparing = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_keep_preparing_the_newer_catch_up() => _preparing.ShouldBeTrue();
    [Fact] void should_keep_the_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_lastHandledBefore);
    [Fact] void should_keep_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextBefore);
    [Fact] void should_keep_the_newer_claim() => _stateStorage.State.ObserverWideClaim.ShouldEqual(_newerClaim);
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(_writesBefore);
    [Fact] void should_not_start_another_catch_up() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
