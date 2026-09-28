// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

/// <summary>
/// Before primary keys followed their tables, a replay swap left the live table holding
/// <c language="csharp">PK_replay-{table}</c> and the backup holding <c language="csharp">PK_{table}</c>.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/> supplying the container.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_an_earlier_release_left_the_primary_keys_with_the_swapped_tables_on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_read_model(fixture)
{
    const string Container = "test_read_models";
    const string LegacyBackup = $"{Container}-20250101120000";
    const string Backup = $"{Container}-20260101120000";

    int _count;
    string? _primaryKey;
    string? _legacyBackupPrimaryKey;
    string? _backupPrimaryKey;

    protected override string ContainerName => Container;

    async Task Establish()
    {
        await Write(5);

        // Leave the database as a swap made by an earlier release did.
        await Execute($"ALTER TABLE \"{Container}\" RENAME TO \"{LegacyBackup}\"");
        await Execute($"CREATE TABLE \"{Container}\" (LIKE \"{LegacyBackup}\" INCLUDING DEFAULTS)");
        await Execute($"ALTER TABLE \"{Container}\" ADD CONSTRAINT \"PK_replay-{Container}\" PRIMARY KEY (\"{_keyColumn}\")");
        await Execute($"INSERT INTO \"{Container}\" SELECT * FROM \"{LegacyBackup}\"");
    }

    async Task Because()
    {
        await Replay(1, Backup);

        _count = await CurrentCount();
        _primaryKey = await PrimaryKeyOf(Container);
        _legacyBackupPrimaryKey = await PrimaryKeyOf(LegacyBackup);
        _backupPrimaryKey = await PrimaryKeyOf(Backup);
    }

    [Fact] void should_hold_what_the_replay_produced() => _count.ShouldEqual(1);
    [Fact] void should_name_the_primary_key_after_the_table() => _primaryKey.ShouldEqual($"PK_{Container}");
    [Fact] void should_name_the_earlier_backups_primary_key_after_its_table() => _legacyBackupPrimaryKey.ShouldEqual($"PK_{LegacyBackup}");
    [Fact] void should_name_the_new_backups_primary_key_after_its_table() => _backupPrimaryKey.ShouldEqual($"PK_{Backup}");
}
