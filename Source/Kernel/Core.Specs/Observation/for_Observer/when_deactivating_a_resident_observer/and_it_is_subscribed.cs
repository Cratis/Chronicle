// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_deactivating_a_resident_observer;

public class and_it_is_subscribed : given.an_observer_with_subscription
{
    async Task Because() => await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Test"), default);

    [Fact] void should_unregister_from_the_supervisor() => _supervisor.Received(1).Unregister(_observerKey.ToString());
}
