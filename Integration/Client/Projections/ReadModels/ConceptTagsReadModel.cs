// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Concepts;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ReadModels;

/// <summary>
/// Read model with a top-level collection of bare concepts, materialized by a projection and persisted to
/// the sink — used to verify a concept collection round-trips through real storage (not just the in-memory
/// harness), mirroring the reducer-based concept-collection coverage.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Tags">The persisted collection of <see cref="StringConcept"/>.</param>
/// <param name="__lastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ConceptTagsReadModel(
    EventSourceId Id,
    IReadOnlyList<StringConcept> Tags,
    EventSequenceNumber __lastHandledEventSequenceNumber = default!);
