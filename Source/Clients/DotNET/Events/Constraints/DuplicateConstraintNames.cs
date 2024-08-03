// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when there are duplicates of constraints with the same name.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="DuplicateConstraintNames"/>.
/// </remarks>
/// <param name="constraintNames">The name of the constraints that is duplicate.</param>
public class DuplicateConstraintNames(IEnumerable<ConstraintName> constraintNames)
    : Exception($"There are duplicates of the following constraints '{string.Join(", ", constraintNames.AsEnumerable())}'")
{
    /// <summary>
    /// Gets the constraint names that are duplicates.
    /// </summary>
    public IEnumerable<ConstraintName> ConstraintNames { get; } = constraintNames;
}
