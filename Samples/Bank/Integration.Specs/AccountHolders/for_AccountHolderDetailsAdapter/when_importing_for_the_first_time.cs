// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.AccountHolders;

namespace Integration.AccountHolders.for_AccountHolderDetailsAdapter;

public class when_importing_for_the_first_time : given.object_ready_for_import
{
    void Because() => context.Import(object_to_import);

    [Fact] void should_append_account_holder_registered() => context.ShouldAppendEvents(new AccountHolderRegistered(first_name, last_name, birth_date));
    [Fact] void should_append_account_holder_address_changed() => context.ShouldAppendEvents(new AccountHolderAddressChanged(address, city, postal_code, country));
}
