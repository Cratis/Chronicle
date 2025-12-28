// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents the MongoDB version of a replay context.
/// </summary>
/// <param name="ReadModel">The read model type.</param>
/// <param name="Generation">The read model generation.</param>
/// <param name="ReadModelName">The read model name.</param>
/// <param name="RevertReadModelName">The revert read model name.</param>
/// <param name="Started">The started date and time.</param>
public record ReplayContext(
    ReadModelIdentifier ReadModel,
    ReadModelGeneration Generation,
    ReadModelName ReadModelName,
    ReadModelName RevertReadModelName,
    DateTimeOffset Started);
