// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;

/// <summary>
/// Converters for replayed models to and from storage.
/// </summary>
public static class ReplayedModelsConverters
{
    /// <summary>
    /// Convert a <see cref="ReplayContext"/> to a <see cref="ReplayedModelOccurrence"/>.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverId"/> for the observer.</param>
    /// <param name="context">The <see cref="ReplayContext"/> to convert.</param>
    /// <returns>The converted <see cref="ReplayedModelOccurrence"/>.</returns>
    public static ReplayedModelOccurrence ToReplayedModelOccurrence(ObserverId observer, ReplayContext context) =>
        new()
        {
            ObserverId = observer.Value,
            ReadModelIdentifier = context.ReadModelIdentifier.Value,
            ReadModelName = context.ReadModel.Value,
            RevertModelName = context.RevertModel.Value,
            Started = context.Started
        };
}
