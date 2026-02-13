// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Represents an occurrence of a replayed read model.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> for the observer that owned the occurrence.</param>
/// <param name="Occurred"><see cref="DateTimeOffset"/> for when it occurred.</param>
/// <param name="Type">The <see cref="ReadModelType"/> of the read model.</param>
/// <param name="ContainerName">Container name of the read model (collection, table, etc.).</param>
/// <param name="RevertContainerName">Container name of the revert read model (collection, table, etc.).</param>
public record ReadModelOccurrence(
    ObserverId ObserverId,
    DateTimeOffset Occurred,
    ReadModelType Type,
    ReadModelContainerName ContainerName,
    ReadModelContainerName RevertContainerName);
