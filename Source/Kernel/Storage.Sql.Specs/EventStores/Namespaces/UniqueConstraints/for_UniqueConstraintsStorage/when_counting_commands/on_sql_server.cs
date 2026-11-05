// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_counting_commands;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_server_claims_storage(fixture)
{
    int _valueIsAllowed;
    int _valueSaveInsert;
    int _valueSaveUpdate;
    int _valueRemoveValue;
    int _valueRemove;
    int _sourceIsAllowed;
    int _sourceSaveInsert;
    int _sourceSaveUpdate;
    int _sourceRemove;

    Task Establish() => Prepare(true);

    async Task Because()
    {
        // Pins the number of commands each operation sends. Each operation reads what it needs with one query and writes with at most one
        // command, as it did when owners were compared by the collation of the database: the exact owner is picked from the rows the same
        // query returns, and the exact-spelling filter is part of the same statement.
        _valueIsAllowed = await Commands(() => _storage.IsAllowed("Alice", _perValue, "one"));
        _valueSaveInsert = await Commands(() => _storage.Save("Alice", _perValue, 1UL, "one"));
        _valueSaveUpdate = await Commands(() => _storage.Save("Alice", _perValue, 1UL, "one", subject: new Subject("subject")));
        _valueRemoveValue = await Commands(() => _storage.RemoveValue("Alice", _perValue, "one"));
        await _storage.Save("Alice", _perValue, 1UL, "one");
        _valueRemove = await Commands(() => _storage.Remove("Alice", _perValue));

        _sourceIsAllowed = await Commands(() => _storage.IsAllowed("Alice", _perSource, "one"));
        _sourceSaveInsert = await Commands(() => _storage.Save("Alice", _perSource, 1UL, "one"));
        _sourceSaveUpdate = await Commands(() => _storage.Save("Alice", _perSource, 2UL, "two"));
        _sourceRemove = await Commands(() => _storage.Remove("Alice", _perSource));
    }

    [Fact] void should_check_a_value_with_one_command() => _valueIsAllowed.ShouldEqual(1);
    [Fact] void should_insert_a_value_with_a_read_and_a_write() => _valueSaveInsert.ShouldEqual(2);
    [Fact] void should_update_a_value_with_a_read_and_a_write() => _valueSaveUpdate.ShouldEqual(2);
    [Fact] void should_remove_a_value_with_one_command() => _valueRemoveValue.ShouldEqual(1);
    [Fact] void should_remove_every_value_of_an_owner_with_one_command() => _valueRemove.ShouldEqual(1);
    [Fact] void should_check_a_source_claim_with_one_command() => _sourceIsAllowed.ShouldEqual(1);
    [Fact] void should_insert_a_source_claim_with_a_read_and_a_write() => _sourceSaveInsert.ShouldEqual(2);
    [Fact] void should_update_a_source_claim_with_a_read_and_a_write() => _sourceSaveUpdate.ShouldEqual(2);
    [Fact] void should_remove_a_source_claim_with_a_read_and_a_write() => _sourceRemove.ShouldEqual(2);
}
