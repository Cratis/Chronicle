// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Domain.Accounts.Debit;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
public sealed class AccountMustBeEmptyAttribute : BusinessRuleAttribute
{
    public override BusinessRuleId Identifier => "bc2042a9-3908-4ac4-8637-b2058ba8cead";

    public double Balance { get; set; }

    public void DefineState(IProjectionBuilderFor<AccountMustBeEmptyAttribute> builder) => builder
        .From<DepositToDebitAccountPerformed>(_ => _
            .Add(m => m.Balance).With(e => e.Amount))
        .From<WithdrawalFromDebitAccountPerformed>(_ => _
            .Subtract(m => m.Balance).With(e => e.Amount));

    public override string FormatErrorMessage(string name) => $"Account must have 0 in balance. It has a balance of {Balance}.";

    protected override bool IsValid(object? value)
    {
        return Balance == 0;
    }
}
