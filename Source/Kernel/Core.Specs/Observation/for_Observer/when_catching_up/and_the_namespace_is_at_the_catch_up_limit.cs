// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

/// <summary>
/// When every client application returns at once, every observer needs a catch-up job. Past the limit an observer stays
/// behind and tries again after a short delay instead of adding another job to the burst.
/// </summary>
public class and_the_namespace_is_at_the_catch_up_limit : given.an_observer_with_subscription
{
    ICatchUpAdmission _admission;
    bool _isPreparingCatchupAfter;
    bool _isBackedOffAfter;

    protected override ICatchUpAdmission CreateCatchUpAdmission()
    {
        _admission = Substitute.For<ICatchUpAdmission>();
        _admission.TryAdmit(Arg.Any<ObserverKey>(), Arg.Any<int>()).Returns(Task.FromResult<IDisposable?>(null));
        return _admission;
    }

    void Establish() => _jobsManager
        .GetJobs(Arg.Any<JobQuery>())
        .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));

    async Task Because()
    {
        await _observer.CatchUp();
        _isPreparingCatchupAfter = await _observer.IsPreparingCatchup();
        _isBackedOffAfter = await _observer.IsCatchUpBackedOff();
    }

    [Fact] void should_ask_with_the_configured_limit() => _admission.Received(1).TryAdmit(_observerKey, _observersConfig.MaxConcurrentCatchUpJobs);

    [Fact] void should_not_start_a_catch_up_job() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] void should_not_be_left_preparing_catch_up() => _isPreparingCatchupAfter.ShouldBeFalse();

    [Fact] void should_wait_before_trying_again() => _isBackedOffAfter.ShouldBeTrue();
}
