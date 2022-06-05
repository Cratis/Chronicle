// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.AccountHolders;

namespace Integration.AccountHolders.for_AccountHolderDetailsAdapter;

public class when_importing_for_the_second_time_with_change_in_address : given.object_ready_for_import
{
    KontoEier object_with_changes = new(
        social_security_number,
        first_name,
        last_name,
        birth_date,
        "Langkaia 2",
        city,
        postal_code,
        country);

    Task Because() => context.Import(object_with_changes);

    [Fact] void should_append_account_holder_address_changed() => context.ShouldAppendEvents(new AccountHolderAddressChanged(object_with_changes.Adresse, city, postal_code, country));
}
