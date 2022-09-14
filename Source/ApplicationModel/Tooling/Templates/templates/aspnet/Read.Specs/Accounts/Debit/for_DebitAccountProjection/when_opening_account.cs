// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;
using Events.Accounts.Debit;

namespace Read.Accounts.Debit;

public class when_opening_account : ProjectionSpecificationFor<DebitAccount>
{
    const string account_id = "ad6f9665-c77d-410b-8968-19ce9b1784e8";
    const string account_name = "My first account";
    const string owner_id = "f3aed250-3200-487b-8a1a-ce1661ea6fee";

    ProjectionResult<DebitAccount> result;

    protected override IProjectionFor<DebitAccount> CreateProjection() => new DebitAccountProjection();

    async Task Because()
    {
        await context.EventLog.Append(account_id, new DebitAccountOpened(account_name, owner_id, true));
        result = await context.GetById(account_id);
    }

    [Fact] void should_set_account_name() => result.Model.Name.Value.ShouldEqual(account_name);
    [Fact] void should_set_owner() => result.Model.Owner.Value.ShouldEqual(Guid.Parse(owner_id));
    [Fact] void should_set_has_card() => ((bool)result.Model.HasCard).ShouldEqual(true);
}
