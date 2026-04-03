// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Converter methods for working with <see cref="Observation.ObserverState"/> converting to and from SQL representations.
/// </summary>
public static class ObserverStateConverters
{
    /// <summary>
    /// Convert to a <see cref="ObserverState">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="Concepts.Observation.Reactors.ReactorDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="ObserverState"/>.</returns>
    public static ObserverState ToSql(this Observation.ObserverState definition) =>
        new()
        {
            Id = definition.Identifier,
            LastHandledEventSequenceNumber = definition.LastHandledEventSequenceNumber,
            RunningState = definition.RunningState,
            ReplayingPartitions = definition.ReplayingPartitions.Select(partition => partition.Value.ToString()!).ToHashSet(),
            CatchingUpPartitions = definition.CatchingUpPartitions.Select(partition => partition.Value.ToString()!).ToHashSet(),
            FailedPartitions = definition.FailedPartitions.Select(fp => new FailedPartition(
                fp.Id.Value.ToString(),
                fp.Partition.Value?.ToString() ?? string.Empty,
                fp.ObserverId.Value,
                fp.Attempts.Select(a => new FailedPartitionAttempt(
                    a.Occurred,
                    a.SequenceNumber.Value,
                    a.Messages,
                    a.StackTrace)),
                fp.IsResolved)).ToList(),
            IsReplaying = definition.IsReplaying
        };

    /// <summary>
    /// Convert to <see cref="Observation.ObserverState"/> from <see cref="ObserverState"/>.
    /// </summary>
    /// <param name="state"><see cref="ObserverState"/> to convert from.</param>
    /// <returns>Converted <see cref="Observation.ObserverState"/>.</returns>
    public static Observation.ObserverState ToKernel(this ObserverState state) =>
        new(
            state.Id,
            state.LastHandledEventSequenceNumber,
            state.RunningState,
            state.ReplayingPartitions.Select(partition => (Key)partition).ToHashSet(),
            state.CatchingUpPartitions.Select(partition => (Key)partition).ToHashSet(),
            state.FailedPartitions.Select(fp => new Concepts.Observation.FailedPartition
            {
                Id = new(Guid.Parse(fp.Id)),
                Partition = new Key(fp.Partition, Properties.ArrayIndexers.NoIndexers),
                ObserverId = new(fp.ObserverId),
                Attempts = fp.Attempts.Select(a => new Concepts.Observation.FailedPartitionAttempt
                {
                    Occurred = a.Occurred,
                    SequenceNumber = a.SequenceNumber,
                    Messages = a.Messages,
                    StackTrace = a.StackTrace
                }),
                IsResolved = fp.IsResolved
            }).ToList(),
            state.IsReplaying);
}
