// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts;
using Concepts.Accounts;
using Events.Accounts.Debit;

namespace Read.Accounts.Debit
{
    [Projection("d1bb5522-5512-42ce-938a-d176536bb01d")]
    public class DebitAccountProjection : IProjectionFor<DebitAccount>
    {
        public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
            builder
                .From<DebitAccountOpened>(_ => _
                    .Set(model => model.Name).To(@event => @event.Name)
                    .Set(model => model.Owner).To(@event => @event.Owner))
                .From<DepositToDebitAccountPerformed>(_ => _
                    .Add(model => model.Balance).With(@event => @event.Amount))
                .From<WithdrawalFromDebitAccountPerformed>(_ => _
                    .Subtract(model => model.Balance).With(@event => @event.Amount));
    }
}
