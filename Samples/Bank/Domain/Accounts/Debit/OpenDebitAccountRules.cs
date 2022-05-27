// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;
using Events.Accounts.Debit;

namespace Domain.Accounts.Debit;

#pragma warning disable RCS1169, IDE0044

public class OpenDebitAccountRules : BusinessRulesFor<OpenDebitAccountRules, OpenDebitAccount>
{
    IEnumerable<AccountName> _accounts = Array.Empty<AccountName>();

    public OpenDebitAccountRules()
    {
        RuleForState(_ => _._accounts).Unique(_ => _.Name).WithMessage("Account with name already exists");
    }

    public override void DefineState(IProjectionBuilderFor<OpenDebitAccountRules> builder) => builder
        .Children(_ => _._accounts, _ => _
            .IdentifiedBy(_ => _)
            .From<DebitAccountOpened>(_ => _.UsingKey(_ => _.Name));

}
