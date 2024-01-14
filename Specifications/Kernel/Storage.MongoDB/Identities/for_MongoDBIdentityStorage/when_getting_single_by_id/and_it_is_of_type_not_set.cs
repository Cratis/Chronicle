// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_single_by_id;

public class and_it_is_of_type_not_set : given.no_identities_registered
{
    Identity result;

    async Task Because() => result = await store.GetSingleFor(IdentityId.NotSet);

    [Fact] void should_be_a_not_set_identity() => result.ShouldEqual(Identity.NotSet);
}
