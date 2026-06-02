// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents the outcome of comparing two constraint definitions.
/// </summary>
/// <param name="RequiresReindex">Whether a reindex is required for this change.</param>
/// <param name="ChangeTypes">The specific change types.</param>
public record ConstraintChange(bool RequiresReindex, IReadOnlyCollection<ConstraintChangeType> ChangeTypes)
{
    /// <summary>
    /// Represents no change.
    /// </summary>
    public static ConstraintChange None { get; } = new(false, [ConstraintChangeType.None]);
}
