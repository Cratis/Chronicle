// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_single_by_id;

public class and_it_exists : given.two_identities_registered
{
    Identity identity;

    async Task Because() => identity = await store.GetSingleFor(first_identity_from_database.Id);

    [Fact] void should_return_an_identity() => identity.ShouldNotBeNull();
    [Fact] void should_return_identity_with_expected_subject() => identity.Subject.ShouldEqual(first_identity_from_database.Subject);
    [Fact] void should_return_identity_with_expected_name() => identity.Name.ShouldEqual(first_identity_from_database.Name);
    [Fact] void should_return_identity_with_expected_user_name() => identity.UserName.ShouldEqual(first_identity_from_database.UserName);
}
