// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Copy;

/// <summary>
/// Exception that gets thrown when appending the copied events to the target event source is refused.
/// </summary>
/// <param name="sourceId">The event source being copied from.</param>
/// <param name="targetId">The event source being copied to.</param>
/// <param name="result">The refused <see cref="IdempotentAppendManyResult"/>.</param>
public class CopyEventSourceEventsFailed(EventSourceId sourceId, EventSourceId targetId, IdempotentAppendManyResult result)
    : Exception($"Copying events from '{sourceId}' to '{targetId}' was refused: {result.Result.ConstraintViolations.Count()} constraint violation(s), {result.Result.ConcurrencyViolations.Count()} concurrency violation(s), {result.Result.Errors.Count()} error(s), {result.Conflicts.Count()} idempotency conflict(s)");
