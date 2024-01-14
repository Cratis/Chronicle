// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_ids;

public class and_they_are_of_type_not_set : given.no_identities_registered
{
    Identity result;

    async Task Because() => result = await store.GetFor(new[] { IdentityId.NotSet, IdentityId.NotSet }.AsEnumerable());

    [Fact] void should_be_an_not_set_identity_with_not_set_behalf_of() => result.ShouldEqual(Identity.NotSet with { OnBehalfOf = Identity.NotSet });
}
