// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents keyed batch settlement without changing the unkeyed batch response.
/// </summary>
/// <param name="Result">The batch validation and storage result.</param>
public record IdempotentAppendManyResult(AppendManyResult Result)
{
    /// <summary>
    /// Gets the settlement outcomes in request order when the batch succeeds.
    /// </summary>
    public IEnumerable<AppendOutcome> Outcomes { get; init; } = [];

    /// <summary>
    /// Gets the durable keys that refused comparison.
    /// </summary>
    public IEnumerable<IdempotencyConflict> Conflicts { get; init; } = [];

    /// <summary>
    /// Gets whether the entire batch settled successfully.
    /// </summary>
    public bool IsSuccess => Result.IsSuccess;
}
