// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_single_identity;

public class and_it_already_exists : given.two_identities_registered
{
    Identity identity;
    IEnumerable<IdentityId> identities;

    void Establish() => identity = new Identity(first_identity_from_database.Subject, first_identity_from_database.Name, first_identity_from_database.UserName);

    async Task Because() => identities = await store.GetFor(identity);

    [Fact] void should_return_only_one_identity() => identities.Count().ShouldEqual(1);
    [Fact] void should_not_insert_the_identity() => inserted_identities.Count.ShouldEqual(0);
    [Fact] void should_return_the_correct_identity() => identities.First().ShouldEqual(first_identity);
}
