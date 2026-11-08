// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.for_MigrationReleaseOrder;

/// <summary>
/// The sink name column is added by a migration that ships in 19.144.0, after 19.143.0 was released without it; EF Core applies it to
/// every database whose history lacks it, and orders it after every migration of an earlier release.
/// </summary>
public class when_ordering_the_sink_name_migration : Specification
{
    string _identifier;
    string[] _ordered;

    void Establish() => _identifier = typeof(v19_144_0).GetCustomAttributes(typeof(MigrationAttribute), false).Cast<MigrationAttribute>().Single().Id;

    void Because() => _ordered = [.. new[] { _identifier, "ES-ReadModelDefinitions-v19_93_0", "NS-Observers-v19_143_0" }.Order(MigrationReleaseOrder.Instance)];

    [Fact] void should_be_identified_by_the_release_it_ships_in() => _identifier.ShouldEqual("ES-ReadModelDefinitions-v19_144_0");
    [Fact] void should_run_after_the_migrations_of_earlier_releases() => _ordered.ShouldEqual(["ES-ReadModelDefinitions-v19_93_0", "NS-Observers-v19_143_0", _identifier]);
}
