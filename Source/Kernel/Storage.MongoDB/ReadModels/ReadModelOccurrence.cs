// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents an occurrence of a replayed read model.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> for the observer that owned the occurrence.</param>
/// <param name="Occurred"><see cref="DateTimeOffset"/> for when it occurred.</param>
/// <param name="Generation">The <see cref="ReadModelGeneration"/> of the read model.</param>
/// <param name="ReadModel">Name of the read model.</param>
/// <param name="RevertReadModel">Name of the revert read model.</param>
public record ReadModelOccurrence(
    ObserverId ObserverId,
    DateTimeOffset Occurred,
    ReadModelGeneration Generation,
    ReadModelName ReadModel,
    ReadModelName RevertReadModel);
