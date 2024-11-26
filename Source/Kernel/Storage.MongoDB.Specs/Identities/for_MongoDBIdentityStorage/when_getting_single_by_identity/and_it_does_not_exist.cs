// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_single_by_identity;

public class and_it_does_not_exist : given.no_identities_registered
{
    Identity identity;
    IdentityId identityId;

    void Establish() => identity = new Identity("Some subject", "Some name", "Some user name");

    async Task Because() => identityId = await store.GetSingleFor(identity);

    [Fact] void should_return_an_id() => identityId.ShouldNotBeNull();
    [Fact] void should_insert_the_identity() => _insertedIdentities.Count.ShouldEqual(1);
    [Fact] void should_insert_the_identity_with_the_correct_subject() => _insertedIdentities[0].Subject.ShouldEqual(identity.Subject);
    [Fact] void should_insert_the_identity_with_the_correct_name() => _insertedIdentities[0].Name.ShouldEqual(identity.Name);
    [Fact] void should_insert_the_identity_with_the_correct_user_name() => _insertedIdentities[0].UserName.ShouldEqual(identity.UserName.ToLowerInvariant());
}
