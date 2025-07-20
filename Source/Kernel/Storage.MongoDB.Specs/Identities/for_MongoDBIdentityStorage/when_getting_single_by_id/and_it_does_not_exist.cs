// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_single_by_id;

public class and_it_does_not_exist : given.no_identities_registered
{
    Identity _result;

    async Task Because() => _result = await store.GetSingleFor(Guid.NewGuid());

    [Fact] void should_be_an_unknown_identity() => _result.ShouldEqual(Identity.Unknown);
}
