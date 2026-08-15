// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_a_partition_fails_and_is_recovered;

/// <summary>
/// The case OnceOnly cannot reach. Recovery re-delivers the event as an ordinary observation, so the handler runs
/// again in full - the second confirmation here proves it did - and only the consumer's receipt, keyed on the
/// delivery identity, keeps the card from being charged twice.
/// </summary>
public class and_the_event_is_delivered_again : given.a_reactor_that_charges_once_per_delivery
{
    ReactorInvocationResult _firstDelivery;
    ReactorInvocationResult _redelivery;

    async Task Because()
    {
        _firstDelivery = await Deliver();
        _redelivery = await Deliver();
    }

    [Fact] void should_fail_the_delivery_that_could_not_confirm() => _firstDelivery.IsFailed.ShouldBeTrue();
    [Fact] void should_succeed_once_the_partition_is_recovered() => _redelivery.IsSuccess.ShouldBeTrue();
    [Fact] void should_charge_the_card_exactly_once() => _payments.Received(1).Charge(OrderId);
    [Fact] void should_have_run_the_handler_for_both_deliveries() => _confirmations.Received(2).Send(OrderId);
}
