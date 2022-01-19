// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections;

namespace Sample.Accounts.Debit
{
    [Projection("4ae2fd6d-0038-4066-8b6e-423c908deee5")]
    public class DebitAccountProjection : IProjectionFor<DebitAccount>
    {
        public void Define(IProjectionBuilderFor<DebitAccount> builder)
        {
            builder
                .From<DebitAccountOpened>(_ => _
                    .Set(_ => _.Name).To(_ => _.Name)
                    .Set(_ => _.Owner).To(_ => _.Owner))
                .From<DepositToDebitAccountPerformed>(_ => _
                    .Add(_ => _.Balance).With(_ => _.Amount))
                .From<WithdrawalFromDebitAccountPerformed>(_ => _
                    .Subtract(_ => _.Balance).With(_ => _.Amount));
        }
    }
}
