// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving_registered_delivery;

public class with_an_effect_epoch : given.a_registered_delivery<given.EpochedDeliveryReactor>
{
    Task Because() => ResolveDeliveries();

    [Fact] void should_take_the_epoch_from_the_reactor_attribute() => _delivery.EffectEpoch.ShouldEqual(5u);
    [Fact] void should_take_the_epoch_without_a_store() => _fallbackDelivery.EffectEpoch.ShouldEqual(5u);
    [Fact] void should_keep_the_epoch_on_replay() => _redelivery.EffectEpoch.ShouldEqual(5u);
}
