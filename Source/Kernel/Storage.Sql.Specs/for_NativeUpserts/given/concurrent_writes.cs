// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions;
using Cratis.Chronicle.Storage.Sql.EventStores.Reactors;
using Microsoft.EntityFrameworkCore;

using KernelFailedPartition = Cratis.Chronicle.Concepts.Observation.FailedPartition;
using KernelReactorDefinition = Cratis.Chronicle.Concepts.Observation.Reactors.ReactorDefinition;

namespace Cratis.Chronicle.Storage.Sql.for_NativeUpserts.given;

public static class concurrent_writes
{
    public static async Task<(int Reactors, int Tokens, int Partitions, bool ReactorPayload, bool TokenPayload, bool PartitionPayload)> Run(IDatabase database)
    {
        const string store = "native-upserts";
        const string ns = "namespace";
        await using var cluster = await database.Cluster();
        await using var definitions = await database.EventStore(store);
        await using var states = await database.Namespace(store, ns);
        var reactors = new ReactorDefinitionsStorage(store, database);
        var tokens = new SqlTokenStorage(database, new JsonSerializerOptions());
        var partitions = new FailedPartitionStorage(store, ns, database);
        var id = FailedPartitionId.New();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writers = Enumerable.Range(0, 8).Select(async writer =>
        {
            await start.Task;
            await reactors.Save(new KernelReactorDefinition("shared-reactor", ReactorOwner.Client, $"sequence-{writer}", [], IsReplayable: true));
            await tokens.Update(new Token { Id = "shared-token", Payload = $"payload-{writer}" });
            await partitions.Save("observer", new Concepts.Observation.FailedPartitions
            {
                Partitions = [new KernelFailedPartition
                {
                    Id = id,
                    ObserverId = "observer",
                    Partition = new Key($"partition-{writer}", ArrayIndexers.NoIndexers)
                }]
            });
        }).ToArray();
        start.SetResult();
        await Task.WhenAll(writers).WaitAsync(TimeSpan.FromSeconds(60));

        var reactor = await definitions.DbContext.Reactors.SingleAsync();
        var token = await cluster.DbContext.Tokens.SingleAsync();
        var partition = await states.DbContext.FailedPartitions.SingleAsync();
        return (await definitions.DbContext.Reactors.CountAsync(), await cluster.DbContext.Tokens.CountAsync(),
            await states.DbContext.FailedPartitions.CountAsync(),
            Enumerable.Range(0, 8).Any(writer => reactor.EventSequenceId == $"sequence-{writer}"),
            Enumerable.Range(0, 8).Any(writer => token.Payload == $"payload-{writer}"),
            Enumerable.Range(0, 8).Any(writer => partition.Partition == $"partition-{writer}"));
    }
}
