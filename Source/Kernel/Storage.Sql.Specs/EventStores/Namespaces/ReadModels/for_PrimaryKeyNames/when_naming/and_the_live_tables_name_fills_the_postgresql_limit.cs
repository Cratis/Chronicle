// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_PrimaryKeyNames.when_naming;

/// <summary>
/// With a 60 character container name <c language="csharp">PK_{table}</c> is exactly 63 bytes, so cutting a
/// backup's primary key name to the limit would give it the very name the live table has.
/// </summary>
/// <remarks>
/// Only the naming is specified here. Replaying a container this long fails before its primary keys matter,
/// because its backup and shadow table names exceed PostgreSQL's limit themselves (issue #4340).
/// </remarks>
public class and_the_live_tables_name_fills_the_postgresql_limit : Specification
{
    const string Container = "read_model_whose_primary_key_name_fills_the_identifier_limit";

    string _live;
    string _backup;

    void Because()
    {
        _live = PrimaryKeyNames.For(DatabaseType.PostgreSql, Container);
        _backup = PrimaryKeyNames.For(DatabaseType.PostgreSql, $"{Container}-20260102120000");
    }

    [Fact] void should_have_a_container_name_of_60_characters() => Container.Length.ShouldEqual(60);
    [Fact] void should_name_the_live_tables_primary_key_after_it_in_full() => _live.ShouldEqual($"PK_{Container}");
    [Fact] void should_give_the_backup_a_distinct_name() => _backup.ShouldNotEqual(_live);
    [Fact] void should_keep_the_backup_within_the_limit() => Encoding.UTF8.GetByteCount(_backup).ShouldEqual(PrimaryKeyNames.PostgreSqlMaxIdentifierBytes);
}
