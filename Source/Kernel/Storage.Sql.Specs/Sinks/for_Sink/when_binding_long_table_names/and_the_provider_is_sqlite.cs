// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_binding_long_table_names;

public class and_the_provider_is_sqlite : given.a_long_replayed_read_model<SqlSinkHarness>
{
    private protected override IReadModelTables Tables => _sqlHarness.Tables;

    Task Because() => PromoteTwice();

    [Fact] void should_publish_the_second_replay() => _current.ShouldEqual(3);
    [Fact] void should_keep_the_unlimited_live_name_unchanged() => _liveName.ShouldEqual(_logical);
    [Fact] void should_not_collapse_roles_or_occurrences() =>
        new[] { _liveName, _replayName, _firstBackupName, _secondBackupName }.Distinct().Count().ShouldEqual(4);
}
