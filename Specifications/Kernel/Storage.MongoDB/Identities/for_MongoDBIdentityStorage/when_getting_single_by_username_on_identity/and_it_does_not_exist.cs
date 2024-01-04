// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_single_by_username_on_identity;

public class and_it_does_not_exist : given.no_identities_registered
{
    Identity identity;
    IdentityId identityId;

    void Establish() => identity = new Identity(string.Empty, string.Empty, "Some user name");

    async Task Because() => identityId = await store.GetSingleFor(identity);

    [Fact] void should_return_an_id() => identityId.ShouldNotBeNull();
    [Fact] void should_insert_the_identity() => inserted_identities.Count.ShouldEqual(1);
    [Fact] void should_insert_the_identity_with_the_correct_subject() => inserted_identities[0].Subject.ShouldEqual(identity.Subject);
    [Fact] void should_insert_the_identity_with_the_correct_name() => inserted_identities[0].Name.ShouldEqual(identity.Name);
    [Fact] void should_insert_the_identity_with_the_correct_user_name() => inserted_identities[0].UserName.ShouldEqual(identity.UserName.ToLowerInvariant());
}
