// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents a replayed read model.
/// </summary>
/// <param name="ReadModel">Read model that was replayed.</param>
/// <param name="Observer">The <see cref="ObserverId"/> for the observer.</param>
/// <param name="Occurrences">Collection of <see cref="ReplayedReadModelOccurrence"/>.</param>
public record ReplayedModel(ReadModelName ReadModel, ObserverId Observer, IEnumerable<ReplayedReadModelOccurrence> Occurrences);
