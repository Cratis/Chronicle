// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_identities;

public class and_they_do_not_exist : given.no_identities_registered
{
    Identity top_level_identity;
    Identity behalf_of_identity;
    IEnumerable<IdentityId> identities;

    void Establish()
    {
        behalf_of_identity = new("Some other subject", "Some other name", "Some other user name");
        top_level_identity = new("Some subject", "Some name", "Some user name", behalf_of_identity);
    }

    async Task Because() => identities = await store.GetFor(top_level_identity);

    [Fact] void should_return_two_identities() => identities.Count().ShouldEqual(2);
    [Fact] void should_insert_two_identities() => _insertedIdentities.Count.ShouldEqual(2);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_subject() => _insertedIdentities[0].Subject.ShouldEqual(top_level_identity.Subject);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_name() => _insertedIdentities[0].Name.ShouldEqual(top_level_identity.Name);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_user_name() => _insertedIdentities[0].UserName.ShouldEqual(top_level_identity.UserName.ToLowerInvariant());
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_subject() => _insertedIdentities.ToArray()[1].Subject.ShouldEqual(behalf_of_identity.Subject);
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_name() => _insertedIdentities.ToArray()[1].Name.ShouldEqual(behalf_of_identity.Name);
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_user_name() => _insertedIdentities.ToArray()[1].UserName.ShouldEqual(behalf_of_identity.UserName.ToLowerInvariant());
}
