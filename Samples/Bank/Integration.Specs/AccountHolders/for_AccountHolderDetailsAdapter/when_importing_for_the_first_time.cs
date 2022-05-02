// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;
using Events.AccountHolders;

namespace Integration.AccountHolders.for_AccountHolderDetailsAdapter;

public class when_importing_for_the_first_time : AdapterSpecificationFor<AccountHolder, KontoEier>
{
    KontoEier object_to_import = new(
        "12345678901",
        "Bør",
        "Børsen",
        new DateTime(1873, 3, 17),
        "Langgata 1",
        "Oslo",
        "0103",
        "Norge");

    protected override IAdapterFor<AccountHolder, KontoEier> CreateAdapter() => new AccountHolderDetailsAdapter();

    void Because() => context.Import(object_to_import);

    [Fact] void should_append_account_holder_registered() => context.ShouldAppendEvents(new AccountHolderRegistered(object_to_import.Fornavn, object_to_import.Etternavn, object_to_import.FodselsDato));
    [Fact] void should_append_account_holder_address_changed() => context.ShouldAppendEvents(new AccountHolderAddressChanged(object_to_import.Adresse, object_to_import.By, object_to_import.PostNr, object_to_import.Land));
}
