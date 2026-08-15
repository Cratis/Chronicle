// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_a_partition_fails_and_is_recovered;

/// <summary>
/// A reactor with a side effect that must not repeat, written the way the seam intends: claim the delivery, do the
/// effect, record it. The confirmation afterwards is the part that fails and takes the partition with it.
/// </summary>
/// <param name="payments">The payment gateway the card is charged through.</param>
/// <param name="confirmations">The confirmations the customer is sent.</param>
/// <param name="receipts">The consumer-owned record of completed deliveries.</param>
public class ChargeCardOnce(IPaymentGateway payments, IOrderConfirmations confirmations, DeliveryReceipts receipts) : IReactor
{
    public async Task ChargeCard(PaymentDue @event, ReactorDelivery delivery)
    {
        if (!receipts.HasCompleted(delivery.Id))
        {
            await payments.Charge(@event.OrderId);
            receipts.Complete(delivery.Id);
        }

        await confirmations.Send(@event.OrderId);
    }
}
