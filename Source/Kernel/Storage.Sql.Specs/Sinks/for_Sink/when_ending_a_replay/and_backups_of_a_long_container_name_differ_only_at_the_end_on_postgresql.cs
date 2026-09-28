// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

/// <summary>
/// A 48 character container name gives backups of exactly 63 bytes, PostgreSQL's identifier limit, so the
/// tables are distinct while <c language="csharp">PK_{backup}</c> is too long and loses the end of the timestamp.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/> supplying the container.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_backups_of_a_long_container_name_differ_only_at_the_end_on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_read_model(fixture)
{
    const string Container = "read_models_with_a_name_of_forty_eight_character";
    const string FirstBackup = $"{Container}-20260101120000";
    const string SecondBackup = $"{Container}-20260101120100";

    int _count;
    string? _primaryKey;
    string? _firstBackupPrimaryKey;
    string? _secondBackupPrimaryKey;

    protected override string ContainerName => Container;

    Task Establish() => Write(5);

    async Task Because()
    {
        await Replay(1, FirstBackup);
        await Replay(2, SecondBackup);

        _count = await CurrentCount();
        _primaryKey = await PrimaryKeyOf(Container);
        _firstBackupPrimaryKey = await PrimaryKeyOf(FirstBackup);
        _secondBackupPrimaryKey = await PrimaryKeyOf(SecondBackup);
    }

    [Fact] void should_have_a_container_name_of_48_characters() => Container.Length.ShouldEqual(48);
    [Fact] void should_hold_what_the_second_replay_produced() => _count.ShouldEqual(2);
    [Fact] void should_name_the_primary_key_after_the_table() => _primaryKey.ShouldEqual(ExpectedPrimaryKeyOf(Container));
    [Fact] void should_name_the_first_backups_primary_key_after_its_table() => _firstBackupPrimaryKey.ShouldEqual(ExpectedPrimaryKeyOf(FirstBackup));
    [Fact] void should_name_the_second_backups_primary_key_after_its_table() => _secondBackupPrimaryKey.ShouldEqual(ExpectedPrimaryKeyOf(SecondBackup));
    [Fact] void should_give_the_backups_distinct_primary_keys() => _firstBackupPrimaryKey.ShouldNotEqual(_secondBackupPrimaryKey);
}
