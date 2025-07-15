// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_single_identity;

public class and_it_already_exists : given.two_identities_registered
{
    Identity _identity;
    IEnumerable<IdentityId> _identities;

    void Establish() => _identity = new Identity(first_identity_from_database.Subject, first_identity_from_database.Name, first_identity_from_database.UserName);

    async Task Because() => _identities = await store.GetFor(_identity);

    [Fact] void should_return_only_one_identity() => _identities.Count().ShouldEqual(1);
    [Fact] void should_not_insert_the_identity() => _insertedIdentities.Count.ShouldEqual(0);
    [Fact] void should_return_the_correct_identity() => _identities.First().ShouldEqual(first_identity);
}
