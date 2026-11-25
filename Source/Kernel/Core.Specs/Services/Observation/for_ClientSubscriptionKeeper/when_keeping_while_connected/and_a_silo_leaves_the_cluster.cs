// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A silo leaving the cluster is what loses subscriptions, so an intact subscription is checked again without waiting
/// for the safety net whenever the supervisor asks after a departure.
/// </summary>
public class and_a_silo_leaves_the_cluster : given.a_client_subscription_keeper
{
    protected override Task<bool> Check(int check) => Task.FromResult(true);

    async Task Because()
    {
        await StartKeeping();
        await _periodicCheck();
        await _checkAfterDeparture();
        await _checkAfterDeparture();
    }

    [Fact] void should_check_every_time_the_supervisor_asks() => _checks.ShouldEqual(3);
}
