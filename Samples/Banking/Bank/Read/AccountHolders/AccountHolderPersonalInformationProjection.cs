// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.AccountHolders;

namespace Read.AccountHolders;

public class AccountHolderPersonalInformationProjection : IImmediateProjectionFor<AccountHolderPersonalInformation>
{
    public ProjectionId Identifier => "c8988dbf-fcf7-4e54-9f27-525fe3ea9126";

    public void Define(IProjectionBuilderFor<AccountHolderPersonalInformation> builder) => builder
        .From<AccountHolderRegistered>(_ => _
            .Set(m => m.FirstName).To(ev => ev.FirstName)
            .Set(m => m.LastName).To(ev => ev.LastName));
}
