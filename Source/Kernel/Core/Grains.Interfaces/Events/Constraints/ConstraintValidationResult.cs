// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents the result of a constraint check.
/// </summary>
public class ConstraintValidationResult
{
    /// <summary>
    /// Gets a successful result.
    /// </summary>
    public static readonly ConstraintValidationResult Success = new();

    /// <summary>
    /// Gets a value indicating whether the result is valid.
    /// </summary>
    public bool IsValid => Violations.Count == 0;

    /// <summary>
    /// Gets the violations that occurred during the check.
    /// </summary>
    public IImmutableList<ConstraintViolation> Violations { get; init; } = [];

    /// <summary>
    /// Creates a failed result with the specified violations.
    /// </summary>
    /// <param name="violations">Collection of <see cref="ConstraintViolation"/>.</param>
    /// <returns>A new <see cref="ConstraintValidationResult"/>.</returns>
    public static ConstraintValidationResult Failed(IEnumerable<ConstraintViolation> violations) => new() { Violations = violations.ToImmutableList() };
}
