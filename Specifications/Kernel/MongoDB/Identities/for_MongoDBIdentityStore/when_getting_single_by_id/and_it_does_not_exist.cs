// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.MongoDB.Identities.for_MongoDBIdentityStore.when_getting_single_by_id;

public class and_it_does_not_exist : given.no_identities_registered
{
    Exception exception;

    async Task Because() => exception = await Catch.Exception(async () => await store.GetSingleFor(Guid.NewGuid()));

    [Fact] void should_throw_unknown_identity_identifier() => exception.ShouldBeOfExactType<UnknownIdentityIdentifier>();
}
