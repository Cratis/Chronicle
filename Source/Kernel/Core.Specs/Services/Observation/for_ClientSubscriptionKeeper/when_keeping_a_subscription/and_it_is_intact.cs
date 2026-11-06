// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_a_subscription;

class and_it_is_intact : Specification
{
    int _sent;

    async Task Because() => await ClientSubscriptionKeeper.KeepSubscription(() => Task.FromResult(true), () =>
    {
        _sent++;
        return Task.CompletedTask;
    });

    [Fact] void should_not_send_the_definition() => _sent.ShouldEqual(0);
}
