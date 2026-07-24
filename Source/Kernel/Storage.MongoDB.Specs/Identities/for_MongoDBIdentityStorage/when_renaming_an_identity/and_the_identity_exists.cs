// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Identities.for_MongoDBIdentityStorage.when_renaming_an_identity;

public class and_the_identity_exists : given.two_identities_registered
{
    const string NewName = "Renamed name";
    Identity _renamed;
    Identity _other;

    async Task Establish() => await store.Populate();

    async Task Because()
    {
        await store.Rename(first_identity_from_database.Subject, NewName);
        _renamed = await store.GetSingleFor(first_identity_from_database.Id);
        _other = await store.GetSingleFor(second_identity_from_database.Id);
    }

    [Fact] void should_change_the_name() => _renamed.Name.ShouldEqual(NewName);
    [Fact] void should_preserve_the_subject_so_the_encryption_key_is_unaffected() => _renamed.Subject.ShouldEqual(first_identity_from_database.Subject);
    [Fact] void should_preserve_the_user_name() => _renamed.UserName.ShouldEqual(first_identity_from_database.UserName);
    [Fact] void should_not_affect_other_identities() => _other.Name.ShouldEqual(second_identity_from_database.Name);
    [Fact] void should_update_the_stored_document() => _collection.Received(1).UpdateOneAsync(Arg.Any<FilterDefinition<MongoDBIdentity>>(), Arg.Any<UpdateDefinition<MongoDBIdentity>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>());
}
