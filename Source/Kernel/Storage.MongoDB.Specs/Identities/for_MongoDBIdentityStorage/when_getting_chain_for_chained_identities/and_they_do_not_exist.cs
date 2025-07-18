// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_identities;

public class and_they_do_not_exist : given.no_identities_registered
{
    Identity _topLevelIdentity;
    Identity _behalfOfIdentity;
    IEnumerable<IdentityId> _identities;

    void Establish()
    {
        _behalfOfIdentity = new("Some other subject", "Some other name", "Some other user name");
        _topLevelIdentity = new("Some subject", "Some name", "Some user name", _behalfOfIdentity);
    }

    async Task Because() => _identities = await store.GetFor(_topLevelIdentity);

    [Fact] void should_return_two_identities() => _identities.Count().ShouldEqual(2);
    [Fact] void should_insert_two_identities() => _insertedIdentities.Count.ShouldEqual(2);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_subject() => _insertedIdentities[0].Subject.ShouldEqual(_topLevelIdentity.Subject);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_name() => _insertedIdentities[0].Name.ShouldEqual(_topLevelIdentity.Name);
    [Fact] void should_insert_the_top_level_identity_with_the_correct_user_name() => _insertedIdentities[0].UserName.ShouldEqual(_topLevelIdentity.UserName.ToLowerInvariant());
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_subject() => _insertedIdentities.ToArray()[1].Subject.ShouldEqual(_behalfOfIdentity.Subject);
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_name() => _insertedIdentities.ToArray()[1].Name.ShouldEqual(_behalfOfIdentity.Name);
    [Fact] void should_insert_the_behalf_of_identity_with_the_correct_user_name() => _insertedIdentities.ToArray()[1].UserName.ShouldEqual(_behalfOfIdentity.UserName.ToLowerInvariant());
}
