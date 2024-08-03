// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents the result of a constraint check.
/// </summary>
public class ConstraintCheckResult
{
    /// <summary>
    /// Gets a successful result.
    /// </summary>
    public static readonly ConstraintCheckResult Success = new();

    /// <summary>
    /// Gets a value indicating whether the check was successful.
    /// </summary>
    public bool IsSuccess => Violations.Count == 0;

    /// <summary>
    /// Gets the violations that occurred during the check.
    /// </summary>
    public ImmutableList<ConstraintViolation> Violations { get; init; } = [];
}
