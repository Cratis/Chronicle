// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_ids;

public class and_they_do_not_exist : given.no_identities_registered
{
    Identity result;

    async Task Because() => result = await store.GetFor(new[] { IdentityId.New(), IdentityId.New() }.AsEnumerable());

    [Fact] void should_be_an_unknown_identity_with_unknown_behalf_of() => result.ShouldEqual(Identity.Unknown with { OnBehalfOf = Identity.Unknown });
}
