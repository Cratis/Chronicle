// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_identities;

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
    [Fact] void should_insert_two_identities() => inserted_identities.Count.ShouldEqual(2);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_subject() => inserted_identities[0].Subject.ShouldEqual(top_level_identity.Subject);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_name() => inserted_identities[0].Name.ShouldEqual(top_level_identity.Name);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_user_name() => inserted_identities[0].UserName.ShouldEqual(top_level_identity.UserName.ToLowerInvariant());
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_subject() => inserted_identities.ToArray()[1].Subject.ShouldEqual(behalf_of_identity.Subject);
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_name() => inserted_identities.ToArray()[1].Name.ShouldEqual(behalf_of_identity.Name);
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_user_name() => inserted_identities.ToArray()[1].UserName.ShouldEqual(behalf_of_identity.UserName.ToLowerInvariant());
}
