// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_a_subscription;

public class and_it_is_not_intact : Specification
{
    int _sent;
    bool _intact;

    async Task Because() => _intact = await ClientSubscriptionKeeper.KeepSubscription(() => Task.FromResult(false), () =>
    {
        _sent++;
        return Task.CompletedTask;
    });

    [Fact] void should_send_the_definition() => _sent.ShouldEqual(1);
    [Fact] void should_not_report_it_intact() => _intact.ShouldBeFalse();
}
