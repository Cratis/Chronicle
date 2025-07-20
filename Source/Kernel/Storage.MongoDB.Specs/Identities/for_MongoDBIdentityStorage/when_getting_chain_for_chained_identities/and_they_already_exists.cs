// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_identities;

public class and_they_already_exists : given.two_identities_registered
{
    Identity _topLevelIdentity;
    Identity _behalfOfIdentity;
    IEnumerable<IdentityId> _identities;

    void Establish()
    {
        _behalfOfIdentity = new(second_identity_from_database.Subject, second_identity_from_database.Name, second_identity_from_database.UserName);
        _topLevelIdentity = new Identity(first_identity_from_database.Subject, first_identity_from_database.Name, first_identity_from_database.UserName, _behalfOfIdentity);
    }

    async Task Because() => _identities = await store.GetFor(_topLevelIdentity);

    [Fact] void should_return_two_identities() => _identities.Count().ShouldEqual(2);
    [Fact] void should_not_insert_the_identity() => _insertedIdentities.Count.ShouldEqual(0);
    [Fact] void should_return_the_correct_top_level_identity() => _identities.First().ShouldEqual(first_identity);
    [Fact] void should_return_the_correct_behalf_of_identity() => _identities.ToArray()[1].ShouldEqual(second_identity);
}
