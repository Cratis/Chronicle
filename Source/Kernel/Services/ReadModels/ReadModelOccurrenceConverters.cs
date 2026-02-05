// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Provides extension methods for converting between domain and contract representations of read model occurrences.
/// </summary>
internal static class ReadModelOccurrenceConverters
{
    /// <summary>
    /// Converts a <see cref="ReadModelOccurrence"/> to a <see cref="Contracts.ReadModels.ReadModelOccurrence"/>.
    /// </summary>
    /// <param name="occurrence">The <see cref="ReadModelOccurrence"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.ReadModels.ReadModelOccurrence"/>.</returns>
    public static Contracts.ReadModels.ReadModelOccurrence ToContract(this ReadModelOccurrence occurrence)
    {
        return new Contracts.ReadModels.ReadModelOccurrence
        {
            ObserverId = occurrence.ObserverId.Value,
            Occurred = occurrence.Occurred!,
            Type = new Contracts.ReadModels.ReadModelType
            {
                Identifier = occurrence.Type.Identifier,
                Generation = occurrence.Type.Generation.Value
            },
            ReadModel = occurrence.ReadModel.Value,
            RevertModel = occurrence.RevertModel.Value
        };
    }
}
