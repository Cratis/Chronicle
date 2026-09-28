// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

/// <summary>
/// With a 60 character container name <c language="csharp">PK_{table}</c> is exactly 63 bytes, so cutting the
/// longer backup's primary key name to the limit gives it the very name the live table needs.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/> supplying the container.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_the_primary_key_name_fills_the_identifier_limit_on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_read_model(fixture)
{
    const string Container = "read_model_whose_primary_key_name_fills_the_identifier_limit";
    const string FirstBackup = $"{Container}-20260101120000";
    const string SecondBackup = $"{Container}-20260102120000";

    int _count;
    string? _primaryKey;
    string? _backupPrimaryKey;

    protected override string ContainerName => Container;

    Task Establish() => Write(5);

    async Task Because()
    {
        await Replay(1, FirstBackup);
        await Replay(2, SecondBackup);

        _count = await CurrentCount();
        _primaryKey = await PrimaryKeyOf(Container);
        _backupPrimaryKey = await PrimaryKeyOf(SecondBackup);
    }

    [Fact] void should_have_a_container_name_of_60_characters() => Container.Length.ShouldEqual(60);
    [Fact] void should_hold_what_the_second_replay_produced() => _count.ShouldEqual(2);
    [Fact] void should_name_the_primary_key_after_the_table() => _primaryKey.ShouldEqual(ExpectedPrimaryKeyOf(Container));
    [Fact] void should_name_the_backups_primary_key_after_its_table() => _backupPrimaryKey.ShouldEqual(ExpectedPrimaryKeyOf(SecondBackup));
}
