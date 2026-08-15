// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_a_partition_fails_and_is_recovered;

/// <summary>
/// The control for the seam, and the gap it exists to fill. Recovery re-delivers as an ordinary observation, not a
/// replay, so a handler marked OnceOnly runs again and charges the card twice. Without this spec the passing one
/// next door proves only that a HashSet works.
/// </summary>
public class and_the_handler_is_marked_once_only_instead : Specification
{
    const string OrderId = "order-42";

    IPaymentGateway _payments;

    void Establish() => _payments = Substitute.For<IPaymentGateway>();

    async Task Because()
    {
        await Deliver();
        await Deliver();
    }

    [Fact] void should_charge_the_card_for_every_delivery() => _payments.Received(2).Charge(OrderId);

    async Task Deliver()
    {
        var reactor = new ChargeCardOnceOnly(_payments);
        var invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(PaymentDue)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ChargeCardOnceOnly),
            new ActivatedArtifact(reactor, typeof(ChargeCardOnceOnly), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());

        await invoker.Invoke(new PaymentDue(OrderId), EventContext.Empty with { ObservationState = EventObservationState.Initial });
    }
}
