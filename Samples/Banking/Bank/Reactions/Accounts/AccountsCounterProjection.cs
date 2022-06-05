// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Debit;

namespace Reactions.Accounts;

public class AccountsCounterProjection : IImmediateProjectionFor<AccountsCounter>
{
    public ProjectionId Identifier => "14e8d5b0-9476-4059-a5e2-09439a98a890";

    public void Define(IProjectionBuilderFor<AccountsCounter> builder) => builder
        .From<DebitAccountOpened>(_ => _.Count(m => m.Count));
}
