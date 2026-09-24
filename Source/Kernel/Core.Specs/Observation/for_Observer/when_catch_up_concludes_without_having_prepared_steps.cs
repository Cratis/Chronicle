// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A catch-up job reports back through CaughtUp whether or not it ever prepared steps of its own. A job that was
/// already running, one that was resumed, and one found with every step already completed and finalized rather than
/// resumed all conclude without a second pass through PrepareSteps - and PrepareSteps was the only thing lowering the
/// preparing flag. The observer was therefore left preparing catch-up forever, dropping every live event and skipping
/// its missed-events check, until the watchdog quarantined it for a strand it could not escape.
/// </summary>
public class when_catch_up_concludes_without_having_prepared_steps : given.an_observer_with_subscription
{
    bool _wasPreparingCatchupBefore;
    bool _isPreparingCatchupAfter;

    Task Establish() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);

    async Task Because()
    {
        await _observer.CatchUp();
        _wasPreparingCatchupBefore = await _observer.IsPreparingCatchup();
        await _observer.CaughtUp(42L);
        _isPreparingCatchupAfter = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_have_been_preparing_catch_up_while_the_job_was_asked_for() => _wasPreparingCatchupBefore.ShouldBeTrue();
    [Fact] void should_no_longer_be_preparing_catch_up() => _isPreparingCatchupAfter.ShouldBeFalse();
}
