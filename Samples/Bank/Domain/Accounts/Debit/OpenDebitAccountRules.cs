// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;
using Events.Accounts.Debit;

namespace Domain.Accounts.Debit;

public class OpenDebitAccountRules : BusinessRulesFor<OpenDebitAccountRules, OpenDebitAccount>
{
    public override BusinessRulesId Identifier => "9c09c285-0eea-4632-ac2d-0d23c7ac10ba";

    public IEnumerable<AccountName> Accounts { get; set; } = Array.Empty<AccountName>();

    public OpenDebitAccountRules()
    {
        RuleForState(_ => _.Accounts)
            .Unique(_ => _.Name)
            .WithMessage("Account with name already exists");
    }

    public override void DefineState(IProjectionBuilderFor<OpenDebitAccountRules> builder) => builder
        .Children(_ => _.Accounts, _ => _
            .IdentifiedBy(_ => _)
            .From<DebitAccountOpened>(_ => _.UsingKey(_ => _.Name)));
}
