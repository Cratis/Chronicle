// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

/// <summary>
/// Represents the result of a concurrency scope check when appending many.
/// </summary>
public class ConcurrencyValidationResults
{
    /// <summary>
    /// Gets a successful result.
    /// </summary>
    public static readonly ConcurrencyValidationResults Success = new();

    /// <summary>
    /// Gets a value indicating whether the result is valid.
    /// </summary>
    public bool IsValid => Violations.Count == 0;

    /// <summary>
    /// Gets the violations that occurred during the check.
    /// </summary>
    public IImmutableList<ConcurrencyViolation> Violations { get; init; } = [];

    /// <summary>
    /// Creates a failed result with the specified violations.
    /// </summary>
    /// <param name="violations">Collection of <see cref="ConcurrencyViolation"/>.</param>
    /// <returns>A new <see cref="ConcurrencyValidationResults"/>.</returns>
    public static ConcurrencyValidationResults Failed(IEnumerable<ConcurrencyViolation> violations) => new()
    {
        Violations = violations.ToImmutableList()
    };
}
