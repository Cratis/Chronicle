// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Converters for <see cref="ReadModelOccurrence"/>.
/// </summary>
public static class ReadModelOccurrenceConverters
{
    /// <summary>
    /// Converts a MongoDB read model occurrence to a kernel read model occurrence.
    /// </summary>
    /// <param name="occurrence">The MongoDB read model occurrence.</param>
    /// <param name="readModel">The read model identifier.</param>
    /// /// <returns>The kernel read model occurrence.</returns>
    public static Chronicle.Storage.ReadModels.ReadModelOccurrence ToKernel(this ReadModelOccurrence occurrence, ReadModelIdentifier readModel)
        => new(
            occurrence.ObserverId,
            occurrence.Occurred,
            new(readModel, occurrence.Generation),
            occurrence.ReadModel,
            occurrence.RevertReadModel);

    /// <summary>
    /// Converts a kernel read model occurrence to a MongoDB read model occurrence.
    /// </summary>
    /// <param name="occurrence">The kernel read model occurrence.</param>
    /// <returns>The MongoDB read model occurrence.</returns>
    public static ReadModelOccurrence ToMongoDB(this Chronicle.Storage.ReadModels.ReadModelOccurrence occurrence)
        => new(
            occurrence.ObserverId,
            occurrence.Occurred,
            occurrence.Type.Generation,
            occurrence.ReadModel,
            occurrence.RevertModel);
}
