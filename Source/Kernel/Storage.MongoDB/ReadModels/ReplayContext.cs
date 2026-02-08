// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents the MongoDB version of a replay context.
/// </summary>
/// <param name="ReadModel">The read model type.</param>
/// <param name="Generation">The read model generation.</param>
/// <param name="ContainerName">The container name of the read model (collection, table, etc.).</param>
/// <param name="RevertContainerName">The container name of the revert read model (collection, table, etc.).</param>
/// <param name="Started">The started date and time.</param>
public record ReplayContext(
    ReadModelIdentifier ReadModel,
    ReadModelGeneration Generation,
    ReadModelContainerName ContainerName,
    ReadModelContainerName RevertContainerName,
    DateTimeOffset Started);
