// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.MongoDB.Identities.for_MongoDBIdentityStore.when_getting_chain_for_chained_ids;

public class and_they_do_not_exist : given.no_identities_registered
{
    Exception exception;

    async Task Because() => exception = await Catch.Exception(async () => await store.GetFor(new[] { IdentityId.New(), IdentityId.New() }.AsEnumerable()));

    [Fact] void should_throw_unknown_identity_identifier() => exception.ShouldBeOfExactType<UnknownIdentityIdentifier>();
}
