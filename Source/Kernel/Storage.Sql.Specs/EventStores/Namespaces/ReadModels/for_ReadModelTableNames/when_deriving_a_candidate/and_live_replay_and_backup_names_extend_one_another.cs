// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_live_replay_and_backup_names_extend_one_another : Specification
{
    string _container = new('x', 63);
    string _live;
    string _replay;
    string _backup;

    void Because()
    {
        _live = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, _container);
        _replay = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, ReadModelTableNames.Replay(_container));
        _backup = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, _container + "-20260101120000-1a2b3c4d");
    }

    [Fact] void should_keep_the_live_name() => _live.ShouldEqual(_container);
    [Fact] void should_separate_live_and_replay() => _live.ShouldNotEqual(_replay);
    [Fact] void should_separate_live_and_backup() => _live.ShouldNotEqual(_backup);
    [Fact] void should_separate_replay_and_backup() => _replay.ShouldNotEqual(_backup);
    [Fact] void should_not_truncate_replay_to_live() => PrimaryKeyNames.TableIdentifier(DatabaseType.PostgreSql, _replay).ShouldNotEqual(_live);
    [Fact] void should_not_truncate_backup_to_live() => PrimaryKeyNames.TableIdentifier(DatabaseType.PostgreSql, _backup).ShouldNotEqual(_live);
    [Fact] void should_not_truncate_backup_to_replay() => PrimaryKeyNames.TableIdentifier(DatabaseType.PostgreSql, _backup).ShouldNotEqual(_replay);
}
