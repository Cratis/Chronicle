// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_a_partition_fails_and_is_recovered.given;

public class a_reactor_that_charges_once_per_delivery : Specification
{
    protected const string OrderId = "order-42";

    protected IPaymentGateway _payments;
    protected IOrderConfirmations _confirmations;
    protected DeliveryReceipts _receipts;
    protected int _confirmationAttempts;

    void Establish()
    {
        _payments = Substitute.For<IPaymentGateway>();
        _confirmations = Substitute.For<IOrderConfirmations>();
        _receipts = new DeliveryReceipts();

        // The first attempt fails after the card has been charged, which is what fails the partition. Recovery
        // then re-delivers the very same event, and this time the confirmation goes through.
        _confirmations.Send(OrderId).Returns(_ => _confirmationAttempts++ == 0
            ? Task.FromException(new ConfirmationFailed())
            : Task.CompletedTask);
    }

    /// <summary>
    /// Delivers the event the way the client does - a fresh reactor instance in a fresh invoker, as the observer
    /// creates a new scope per batch - so nothing but the consumer's own receipts survives between deliveries.
    /// </summary>
    /// <returns>The <see cref="ReactorInvocationResult"/> of the delivery.</returns>
    protected async Task<ReactorInvocationResult> Deliver()
    {
        var reactor = new ChargeCardOnce(_payments, _confirmations, _receipts);
        var invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(PaymentDue)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ChargeCardOnce),
            new ActivatedArtifact(reactor, typeof(ChargeCardOnce), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());

        return await invoker.Invoke(new PaymentDue(OrderId), Context());
    }

    /// <summary>
    /// Builds the context the event arrives in, rebuilt from scratch for every delivery the way recovery rebuilds
    /// it by re-reading the event out of the sequence.
    /// </summary>
    /// <returns>The <see cref="EventContext"/> for the event.</returns>
    static EventContext Context() =>
        EventContext.From(
            "orders",
            "default",
            typeof(PaymentDue).GetEventType(),
            EventSourceType.Default,
            OrderId,
            EventStreamType.All,
            EventStreamId.Default,
            7UL,
            CorrelationId.NotSet);
}
