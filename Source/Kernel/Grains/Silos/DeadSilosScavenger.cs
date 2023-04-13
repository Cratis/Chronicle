// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Orleans.Configuration;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;

namespace Aksio.Cratis.Kernel.Grains.Silos;

/// <summary>
/// Represents an implementation of <see cref="IDeadSilosScavenger"/>.
/// </summary>
[StatelessWorker(1)]
public class DeadSilosScavenger : Grain, IDeadSilosScavenger
{
    readonly KernelConfiguration _configuration;
    readonly ILogger<DeadSilosScavenger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadSilosScavenger"/> class.
    /// </summary>
    /// <param name="configuration"><see cref="KernelConfiguration"/> holding the configuration.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public DeadSilosScavenger(
        KernelConfiguration configuration,
        ILogger<DeadSilosScavenger> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Start()
    {
        if (_configuration.Cluster.Options is AzureStorageClusterOptions options)
        {
            RegisterTimer(_ => CleanUp(), null!, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task CleanUp()
    {
        if (_configuration.Cluster.Options is AzureStorageClusterOptions options)
        {
            var deadSilos = 0;
            _logger.LookForDeadSilos();

            var client = new TableClient(
                options.ConnectionString,
                options.TableName);

            var result = client.QueryAsync<OrleansSiloInfo>(filter: "Status eq 'Dead'");
            await foreach (var page in result.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    _logger.RemovingDeadSiloFromClusterInfo(entity.Address);
                    await client.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, entity.ETag);
                    deadSilos++;
                }
            }

            if (deadSilos == 0) _logger.NoDeadSilos();
        }
    }
}
