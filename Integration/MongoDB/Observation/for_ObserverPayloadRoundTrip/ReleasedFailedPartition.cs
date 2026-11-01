// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.MongoDB;
using NSubstitute;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Makes the released kernel persist a failed partition through an isolated client reactor.
/// </summary>
public static class ReleasedFailedPartition
{
    /// <summary>
    /// Handles a successful partition before failing one chosen event source and awaiting its persisted failure.
    /// </summary>
    /// <param name="fixture">The running released kernel and its MongoDB.</param>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The database, observer, failed partition, and successfully handled partition.</returns>
    /// <exception cref="InvalidOperationException">The successful append or its observation did not succeed.</exception>
    public static async Task<(string Database, string ObserverId, string Partition, string SucceedingPartition)> Produce(
        ReleasedKernelFixture fixture,
        CancellationToken cancellationToken)
    {
        const string observerId = "released-failure-reactor";
        const string partition = "failed-partition";
        const string succeedingPartition = "successful-partition";
        var eventStoreName = $"released-failure-{Guid.NewGuid():N}";
        var options = ChronicleOptions.FromConnectionString(new ChronicleConnectionString($"{fixture.KernelAddress}/?skipTlsValidation=true&skipCompatibilityCheck=true"));
        options.SkipCompatibilityCheck = true;
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(ReleasedPartitionRecorded)]);
        using var client = new ChronicleClient(options, artifactsProvider: artifacts);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            var eventStore = await client.GetEventStore(eventStoreName).WaitAsync(deadline.Token);
            var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await eventStore.Reactors.Register(observerId, builder => builder.On<ReleasedPartitionRecorded>((_, context) =>
            {
                if (context.EventSourceId.Value == partition)
                {
                    throw new InvalidOperationException("Released-kernel failed-partition probe.");
                }
                if (context.EventSourceId.Value == succeedingPartition)
                {
                    handled.TrySetResult();
                }
                return Task.CompletedTask;
            })).WaitAsync(deadline.Token);

            var successfulAppend = await eventStore.EventLog.Append(succeedingPartition, new ReleasedPartitionRecorded("successful")).WaitAsync(deadline.Token);
            if (!successfulAppend.IsSuccess) throw new InvalidOperationException("The successful partition's append was rejected.");
            await handled.Task.WaitAsync(deadline.Token);
            var completion = await successfulAppend.WaitForCompletion(TimeSpan.FromSeconds(30)).WaitAsync(deadline.Token);
            if (!completion.IsSuccess || completion.TimedOut) throw new InvalidOperationException("The released kernel did not acknowledge successful observation.");

            var failedAppend = await eventStore.EventLog.Append(partition, new ReleasedPartitionRecorded("failed")).WaitAsync(deadline.Token);
            if (!failedAppend.IsSuccess) throw new InvalidOperationException("The failing partition's append was rejected.");
            var databaseName = DatabaseNames.ForEventStoreNamespace(eventStoreName, eventStore.Namespace.Value);
            await ReleasedFailedPartitionWait.WaitFor(fixture.Client.GetDatabase(databaseName), observerId, partition, deadline.Token);
            return (databaseName, observerId, partition, succeedingPartition);
        }
        finally
        {
            client.EvictEventStores();
        }
    }
}
