// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A silo leaving the cluster is what loses subscriptions, so an intact subscription is checked again without waiting
/// for the safety net - spread over one revise interval - and every revise interval while the cluster settles.
/// </summary>
public class and_a_silo_leaves_the_cluster : given.a_client_subscription_keeper
{
    /// <summary>
    /// The checks between the first after the departure and the last revise interval of the settle window.
    /// </summary>
    static readonly int _checksWhileSettling = (int)Math.Ceiling((Settle - Spread - Revise) / Revise);

    TimeSpan _waitAfterTheDeparture;
    List<TimeSpan> _waitsWhileSettling;

    protected override Task<bool> Check(int check) => Task.FromResult(true);

    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        await _clock.NextWait();
        SiloLeaves();
        _waitAfterTheDeparture = await _clock.Elapse();
        _waitsWhileSettling = await ElapseAtLeast(Settle - Spread - Revise);
        await _clock.NextWait();
    }

    [Fact] void should_spread_the_first_check() => _waitAfterTheDeparture.ShouldEqual(Spread);
    [Fact] void should_check_every_revise_interval_while_settling() => _waitsWhileSettling.SequenceEqual(Enumerable.Repeat(Revise, _checksWhileSettling)).ShouldBeTrue();
    [Fact] void should_check_after_every_wait() => _checks.ShouldEqual(2 + _checksWhileSettling);
}
