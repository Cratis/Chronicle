// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.AccountHolders;

namespace Read.AccountHolders;

public class AccountHolderProjection : IProjectionFor<AccountHolder>
{
    public ProjectionId Identifier => "4e53ba3f-c7bc-4129-a727-867a267b0941";

    public void Define(IProjectionBuilderFor<AccountHolder> builder) => builder
        .From<AccountHolderRegistered>(_ => _
            .Set(m => m.FirstName).To(ev => ev.FirstName)
            .Set(m => m.LastName).To(ev => ev.LastName))
        .From<AccountHolderAddressChanged>(_ => _
            .Set(m => m.Address).To(ev => ev.Address)
            .Set(m => m.City).To(ev => ev.City)
            .Set(m => m.PostalCode).To(ev => ev.PostalCode)
            .Set(m => m.Country).To(ev => ev.Country));
}
