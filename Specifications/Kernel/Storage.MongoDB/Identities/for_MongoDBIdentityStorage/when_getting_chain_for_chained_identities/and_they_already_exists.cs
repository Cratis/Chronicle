// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_identities;

public class and_they_already_exists : given.two_identities_registered
{
    Identity top_level_identity;
    Identity behalf_of_identity;
    IEnumerable<IdentityId> identities;

    void Establish()
    {
        behalf_of_identity = new(second_identity_from_database.Subject, second_identity_from_database.Name, second_identity_from_database.UserName);
        top_level_identity = new Identity(first_identity_from_database.Subject, first_identity_from_database.Name, first_identity_from_database.UserName, behalf_of_identity);
    }

    async Task Because() => identities = await store.GetFor(top_level_identity);

    [Fact] void should_return_two_identities() => identities.Count().ShouldEqual(2);
    [Fact] void should_not_insert_the_identity() => inserted_identities.Count.ShouldEqual(0);
    [Fact] void should_return_the_correct_top_level_identity() => identities.First().ShouldEqual(first_identity);
    [Fact] void should_return_the_correct_behalf_of_identity() => identities.ToArray()[1].ShouldEqual(second_identity);
}
