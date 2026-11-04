// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

public class and_asking_a_fresh_namespace_for_its_replays : given.a_database_without_read_model_storage
{
    IEnumerable<ReadModelOccurrence> _occurrences;

    async Task Because() => _occurrences = await new ReplayedModelsStorage("store", "fresh-namespace", _database).GetOccurrences("model");

    [Fact] void should_have_no_occurrences() => _occurrences.ShouldBeEmpty();
    [Fact] void should_leave_the_namespace_unprovisioned() => string.Join(',', FilesOnDisk).ShouldEqual("cluster.db");
}
