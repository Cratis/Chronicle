// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Public.Accounts.Debit;

public class AccountBalanceProjection : IImmediateProjectionFor<Account>
{
    public ProjectionId Identifier => "e53ae165-1658-486c-b03c-7c6041428851";

    public void Define(IProjectionBuilderFor<Account> builder) => builder
        .From<DepositToDebitAccountPerformed>(_ => _
            .Add(m => m.Balance).With(e => e.Amount))
        .From<WithdrawalFromDebitAccountPerformed>(_ => _
            .Subtract(m => m.Balance).With(e => e.Amount));
}
