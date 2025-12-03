// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_getting_single_by_id;

public class and_it_exists : given.two_identities_registered
{
    Identity _identity;

    async Task Because() => _identity = await store.GetSingleFor(first_identity_from_database.Id);

    [Fact] void should_return_an_identity() => _identity.ShouldNotBeNull();
    [Fact] void should_return_identity_with_expected_subject() => _identity.Subject.ShouldEqual(first_identity_from_database.Subject);
    [Fact] void should_return_identity_with_expected_name() => _identity.Name.ShouldEqual(first_identity_from_database.Name);
    [Fact] void should_return_identity_with_expected_user_name() => _identity.UserName.ShouldEqual(first_identity_from_database.UserName);
}
