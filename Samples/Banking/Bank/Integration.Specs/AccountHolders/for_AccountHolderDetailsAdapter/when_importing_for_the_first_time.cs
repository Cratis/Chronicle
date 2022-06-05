// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.AccountHolders;

namespace Integration.AccountHolders.for_AccountHolderDetailsAdapter;

public class when_importing_for_the_first_time : given.object_ready_for_import
{
    AccountHolder result;

    async Task Because()
    {
        await context.Import(object_to_import);
        result = await context.Projection.GetById(social_security_number);
    }

    [Fact] void should_append_account_holder_registered() => context.ShouldAppendEvents(new AccountHolderRegistered(first_name, last_name, birth_date));
    [Fact] void should_append_account_holder_address_changed() => context.ShouldAppendEvents(new AccountHolderAddressChanged(address, city, postal_code, country));
    [Fact] void should_project_all_properties() => result.ShouldEqual(new AccountHolder(first_name, last_name, birth_date, social_security_number, address, city, postal_code, country));
}
