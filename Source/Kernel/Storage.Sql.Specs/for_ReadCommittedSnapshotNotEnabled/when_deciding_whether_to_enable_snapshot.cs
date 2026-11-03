// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ReadCommittedSnapshotNotEnabled;

public class when_deciding_whether_to_enable_snapshot : Specification
{
    [Fact] void should_skip_azure_sql_database() => SqlServerReadCommittedSnapshot.ShouldEnable(5, false).ShouldBeFalse();
    [Fact] void should_skip_an_already_enabled_database() => SqlServerReadCommittedSnapshot.ShouldEnable(3, true).ShouldBeFalse();
    [Fact] void should_enable_a_new_sql_server_database() => SqlServerReadCommittedSnapshot.ShouldEnable(3, false).ShouldBeTrue();
    [Fact] void should_enable_a_new_managed_instance_database() => SqlServerReadCommittedSnapshot.ShouldEnable(8, false).ShouldBeTrue();
}
