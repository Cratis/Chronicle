// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_chain_for_chained_ids;

public class and_they_already_exists : given.two_identities_registered
{
    Identity identity;

    async Task Because() => identity = await store.GetFor(new[] { first_identity, second_identity }.AsEnumerable());

    [Fact] void should_return_top_level_identity_with_expected_subject() => identity.Subject.ShouldEqual(first_identity_from_database.Subject);
    [Fact] void should_return_top_level_identity_with_expected_name() => identity.Name.ShouldEqual(first_identity_from_database.Name);
    [Fact] void should_return_top_level_identity_with_expected_user_name() => identity.UserName.ShouldEqual(first_identity_from_database.UserName);
    [Fact] void should_return_behalf_of_identity_with_expected_subject() => identity.OnBehalfOf.Subject.ShouldEqual(second_identity_from_database.Subject);
    [Fact] void should_return_behalf_of_identity_with_expected_name() => identity.OnBehalfOf.Name.ShouldEqual(second_identity_from_database.Name);
    [Fact] void should_return_behalf_of_identity_with_expected_user_name() => identity.OnBehalfOf.UserName.ShouldEqual(second_identity_from_database.UserName);
}
