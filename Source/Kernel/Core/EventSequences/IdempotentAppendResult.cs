// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents a keyed append settlement without changing the unkeyed append response.
/// </summary>
/// <param name="Result">The append validation and storage result.</param>
/// <param name="Outcome">How the keyed append was settled.</param>
public record IdempotentAppendResult(AppendResult Result, AppendOutcome Outcome = AppendOutcome.Committed)
{
    /// <summary>
    /// Gets the durable keys that refused comparison.
    /// </summary>
    public IEnumerable<IdempotencyConflict> Conflicts { get; init; } = [];

    /// <summary>
    /// Gets whether the append settled successfully.
    /// </summary>
    public bool IsSuccess => Result.IsSuccess;
}
