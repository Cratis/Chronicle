// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_getting_or_adding;

public class and_the_insert_fails_without_a_winner : Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.given.a_migrated_namespace_database
{
    EncryptionKeyStorage _storage;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    Exception? _exception;

    async Task Establish()
    {
        _storage = new EncryptionKeyStorage(_database);
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_key_insert BEFORE INSERT ON EncryptionKeys BEGIN SELECT RAISE(ABORT, 'key insert failed'); END");
    }

    async Task Because() => _exception = await Catch.Exception(() => _storage.GetOrAddFor(
        EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new([1], [2])));

    [Fact] void should_rethrow_the_database_failure() => _exception.ShouldBeOfExactType<DbUpdateException>();
    [Fact] async Task should_not_return_an_unpersisted_key() => (await _storage.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)).ShouldBeNull();
}
