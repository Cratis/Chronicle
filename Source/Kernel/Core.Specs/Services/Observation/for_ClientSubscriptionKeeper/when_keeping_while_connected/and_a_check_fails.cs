// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A silo that is still leaving the cluster can fail a check or two; the subscription is checked again after one revise
/// interval rather than left to the safety net.
/// </summary>
public class and_a_check_fails : given.a_client_subscription_keeper
{
    TimeSpan _waitAfterTheFailure;

    protected override Task<bool> Check(int check) =>
        check == 1 ? Task.FromException<bool>(new InvalidOperationException("The silo is leaving")) : Task.FromResult(true);

    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        _waitAfterTheFailure = await _clock.Elapse();
        await _clock.NextWait();
    }

    [Fact] void should_check_again_after_one_revise_interval() => _waitAfterTheFailure.ShouldEqual(Revise);
    [Fact] void should_keep_checking() => _checks.ShouldEqual(2);
}
