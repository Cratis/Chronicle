// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents a replayed read model.
/// </summary>
/// <param name="ReadModel">Read model that was replayed.</param>
/// <param name="Occurrences">Collection of <see cref="ReadModelOccurrence"/>.</param>
public record ReplayedReadModel(
    ReadModelIdentifier ReadModel,
    IEnumerable<ReadModelOccurrence> Occurrences);
