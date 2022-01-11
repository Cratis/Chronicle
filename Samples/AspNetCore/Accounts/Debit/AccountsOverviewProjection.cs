// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;

namespace Sample.Accounts.Debit
{
    //[Projection("8fdaaf0d-1291-47b7-b661-2eeba340a520")]
    public class AccountsOverviewProjection //: IProjectionFor<AccountsOverview>
    {
        public void Define(IProjectionBuilderFor<AccountsOverview> builder)
        {
            builder
                .Children(_ => _.DebitAccounts, _ => _
                    .IdentifiedBy(_ => _.Id)
                    .From<DebitAccountOpened>(_ => _
                        .UsingParentKey(_ => _.Owner)
                        .Set(_ => _.Name).To(_ => _.Name)));
        }
    }
}
