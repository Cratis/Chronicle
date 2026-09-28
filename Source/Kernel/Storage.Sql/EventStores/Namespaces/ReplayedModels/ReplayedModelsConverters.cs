// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;

/// <summary>
/// Converters for replayed models to and from storage.
/// </summary>
public static class ReplayedModelsConverters
{
    /// <summary>
    /// Convert a <see cref="ReadModelOccurrence"/> to a <see cref="ReplayedModelOccurrence"/>.
    /// </summary>
    /// <param name="occurrence">The <see cref="ReadModelOccurrence"/> to convert.</param>
    /// <returns>The converted <see cref="ReplayedModelOccurrence"/>.</returns>
    public static ReplayedModelOccurrence ToReplayedModelOccurrence(ReadModelOccurrence occurrence) =>
        new()
        {
            ObserverId = occurrence.ObserverId.Value,
            ReadModelIdentifier = occurrence.Type.Identifier.Value,
            Generation = occurrence.Type.Generation.Value,
            ReadModelName = occurrence.ContainerName.Value,
            RevertModelName = occurrence.RevertContainerName.Value,
            Started = ToStoredPrecision(occurrence.Occurred)
        };

    /// <summary>
    /// Convert a <see cref="ReplayedModelOccurrence"/> to a <see cref="ReadModelOccurrence"/>.
    /// </summary>
    /// <param name="occurrence">The <see cref="ReplayedModelOccurrence"/> to convert.</param>
    /// <returns>The converted <see cref="ReadModelOccurrence"/>.</returns>
    public static ReadModelOccurrence ToReadModelOccurrence(ReplayedModelOccurrence occurrence) =>
        new(
            new(occurrence.ObserverId),
            occurrence.Started,
            new ReadModelType(new ReadModelIdentifier(occurrence.ReadModelIdentifier), new ReadModelGeneration(occurrence.Generation)),
            new(occurrence.ReadModelName),
            new(occurrence.RevertModelName));

    /// <summary>
    /// Truncates a replay start to whole microseconds, the finest precision every SQL provider stores.
    /// </summary>
    /// <param name="started">When the replay started.</param>
    /// <returns>The start as it is stored.</returns>
    /// <remarks>
    /// PostgreSQL keeps microseconds while <see cref="DateTimeOffset"/> has 100 nanosecond ticks, so a start held
    /// in memory would otherwise never equal the one read back, and the start is part of the row's key.
    /// </remarks>
    internal static DateTimeOffset ToStoredPrecision(DateTimeOffset started) =>
        started.AddTicks(-(started.Ticks % TimeSpan.TicksPerMicrosecond));
}
