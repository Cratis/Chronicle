// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationAdmission.given;

public class a_sqlite_provider : for_Database.given.a_sqlite_database
{
    protected Database Provider => _database;

    protected EventStoreName Name => new("orders");
    protected IOptions<ChronicleOptions> OptionsForProvider => Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = _connectionString } });

    protected async Task Advance(uint protocol)
    {
        var floor = new GenerationFloor(new(protocol), new(protocol), [GenerationCapability.FloorAdmission], new(Guid.NewGuid()), 0, default, "", "");
        await new EventStores.Generations.GenerationFloorStorage(() => Provider.EventStore(Name)).Advance(new(floor, 0, "spec", "floor", new(true, "uniform")));
    }

    protected async Task Admit()
    {
        var factories = Substitute.For<IInstancesOf<ISinkFactory>>();
        factories.GetEnumerator().Returns(_ => Enumerable.Empty<ISinkFactory>().GetEnumerator());
        using var cluster = new ClusterStorage(Provider, factories, Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(), new JsonSerializerOptions(), Substitute.For<Json.IExpandoObjectConverter>());
        var storage = new Chronicle.Storage.Storage(cluster, Substitute.For<ISystemStorage>(), factories);
        await new GenerationFloorAdmission(storage).Admit(CancellationToken.None);
    }
}
