// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_single_by_identity;

public class and_it_already_exists_without_subject : given.two_identities_without_subject_registered
{
    Identity _identity;
    IdentityId _identityId;

    void Establish() => _identity = new Identity("831176bb-79ad-4558-b4ca-b49905d1a843", first_identity_from_database.Name, first_identity_from_database.UserName);

    async Task Because() => _identityId = await store.GetSingleFor(_identity);

    [Fact] void should_return_an_id() => _identityId.ShouldNotBeNull();
    [Fact] void should_not_insert_the_identity() => _insertedIdentities.Count.ShouldEqual(0);
    [Fact] void should_return_the_correct_identity() => _identityId.ShouldEqual(first_identity);
}
