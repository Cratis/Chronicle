// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_upgrading_legacy_owners;

[Collection(SqlServerCollection.Name)]
public class and_the_rows_are_pending(SqlServerFixture fixture) : for_UniqueConstraintMigrator.given.a_legacy_server_table
{
    readonly string _valueA = new('a', 64);
    readonly string _valueB = new('b', 64);
    UniqueConstraintsStorage _storage;
    UniqueConstraintDefinition _definition;
    long _rows;
    long _pending;
    long _keyColumns;
    long _rowsAfterSecondRun;
    long _pendingAfterSecondRun;
    bool _otherSpellingMayHoldTheValue;
    Exception? _otherSpellingError;
    bool _legacyOwnerKeepsItsValue;
    long _rowsAfterOtherSpellingSaved;
    long _rowsAfterOtherSpellingRemoved;
    bool _legacyValueStillHeld;
    string[] _legacyOwnerValues = [];

    async Task Establish()
    {
        var connectionString = await fixture.CreateDatabase();
        await CreateLegacyTable(connectionString, true, () => fixture.QueueDatabaseDrop(connectionString));
        await AddClaim("Alice", _valueA);
        await AddClaim("Bob", _valueB);
        var database = Substitute.For<IDatabase>();
        database.UniqueConstraintTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<UniqueConstraintDbContext>(new(_options, _tableName, _migrator), () => { })));
        _storage = new("store", "namespace", EventSequenceId.Log, database);
        _definition = new("unique", []);
    }

    async Task Because()
    {
        await _context.EnsureTableExists();
        _rows = await _context.Entries.CountAsync();
        _pending = await Scalar($"SELECT COUNT(*) FROM {_table} WHERE [ExactSpelling] = 0");
        _keyColumns = await Scalar($"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('{_tableName}') AND name = 'ExactKey'");
        _migrator.ClearMigrationCache(_connectionString);
        await _context.EnsureTableExists();
        _rowsAfterSecondRun = await _context.Entries.CountAsync();
        _pendingAfterSecondRun = await Scalar($"SELECT COUNT(*) FROM {_table} WHERE [ExactSpelling] = 0");

        (_otherSpellingMayHoldTheValue, _) = await _storage.IsAllowed("alice", _definition, _valueA);
        _otherSpellingError = await Catch.Exception(() => _storage.Save("alice", _definition, 50UL, _valueB + "x"));
        _rowsAfterOtherSpellingSaved = await _context.Entries.CountAsync();
        (_legacyOwnerKeepsItsValue, _) = await _storage.IsAllowed("Alice", _definition, _valueA);
        _legacyOwnerValues = (await _storage.GetFor("Alice", _definition)).Select(_ => _.Value).ToArray();
        await _storage.Remove("alice", _definition);
        _rowsAfterOtherSpellingRemoved = await _context.Entries.CountAsync();
        (var free, _) = await _storage.IsAllowed("third", _definition, _valueA);
        _legacyValueStillHeld = !free;
    }

    [Fact] void should_keep_every_row() => _rows.ShouldEqual(2);
    [Fact] void should_add_the_owner_hash() => _keyColumns.ShouldEqual(1);
    [Fact] void should_mark_every_existing_row_as_pending() => _pending.ShouldEqual(2);
    [Fact] void should_be_idempotent() => (_rowsAfterSecondRun == 2 && _pendingAfterSecondRun == 2).ShouldBeTrue();
    [Fact] void should_refuse_another_spelling_the_value_of_a_pending_row() => _otherSpellingMayHoldTheValue.ShouldBeFalse();
    [Fact] void should_let_another_spelling_save_without_error() => _otherSpellingError.ShouldBeNull();
    [Fact] void should_keep_a_row_for_the_other_spelling() => _rowsAfterOtherSpellingSaved.ShouldEqual(3);
    [Fact] void should_leave_the_pending_row_with_its_owner() => (_legacyOwnerKeepsItsValue && _legacyOwnerValues.Length == 1 && _legacyOwnerValues[0] == _valueA).ShouldBeTrue();
    [Fact] void should_not_release_the_pending_row_when_another_spelling_is_removed() => (_rowsAfterOtherSpellingRemoved == 2 && _legacyValueStillHeld).ShouldBeTrue();
}
