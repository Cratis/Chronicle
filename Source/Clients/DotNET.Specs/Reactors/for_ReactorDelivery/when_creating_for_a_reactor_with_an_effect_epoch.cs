// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery;

public class when_creating_for_a_reactor_with_an_effect_epoch : given.a_delivery
{
    ReactorDelivery _epoched;

    void Because() => _epoched = ReactorDelivery.For(new EpochedOrderConfirmations(), _context);

    [Fact] void should_take_the_epoch_from_the_reactor_attribute() => _epoched.EffectEpoch.ShouldEqual(4u);
    [Fact] void should_default_the_epoch_to_zero_without_the_attribute() => _delivery.EffectEpoch.ShouldEqual(0u);
}
