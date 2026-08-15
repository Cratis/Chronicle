// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_a_partition_fails_and_is_recovered;

/// <summary>
/// The same side effect guarded the way a reader might expect OnceOnly to guard it. It is the control for the
/// seam: OnceOnly takes the handler out of replay and nothing else, so recovery still runs it.
/// </summary>
/// <param name="payments">The payment gateway the card is charged through.</param>
public class ChargeCardOnceOnly(IPaymentGateway payments) : IReactor
{
    [OnceOnly]
    public Task ChargeCard(PaymentDue @event) => payments.Charge(@event.OrderId);
}
