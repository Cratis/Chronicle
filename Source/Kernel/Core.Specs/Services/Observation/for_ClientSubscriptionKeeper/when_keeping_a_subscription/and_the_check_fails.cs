// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_a_subscription;

class and_the_check_fails : Specification
{
    int _sent;

    async Task Because() => await ClientSubscriptionKeeper.KeepSubscription(() => Task.FromException<bool>(new InvalidOperationException("an older silo")), () =>
    {
        _sent++;
        return Task.CompletedTask;
    });

    [Fact] void should_send_the_definition() => _sent.ShouldEqual(1);
}
