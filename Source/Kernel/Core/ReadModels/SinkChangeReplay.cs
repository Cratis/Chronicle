// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Recommendations;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the replays recorded for a read model moved to another sink.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
/// <param name="ReadModel">The <see cref="ReadModelIdentifier"/> of the read model.</param>
/// <param name="Recommendations">The recorded recommendations, by namespace.</param>
public record SinkChangeReplay(EventStoreName EventStore, ReadModelIdentifier ReadModel, IReadOnlyList<(EventStoreNamespaceName Namespace, RecommendationId Recommendation)> Recommendations);
