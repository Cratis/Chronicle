// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents the request for reindexing constraint indexes for an event sequence.
/// </summary>
/// <param name="EventSequenceId">The event sequence identifier to reindex for.</param>
/// <param name="Changes">The constraint changes to reindex.</param>
public record ReindexConstraintsRequest(
    EventSequenceId EventSequenceId,
    IReadOnlyCollection<ConstraintDefinitionChange> Changes) : IJobRequest;
