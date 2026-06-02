// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition for the closed-stream constraint.
/// </summary>
public class ClosedStreamConstraintDefinition : IConstraintDefinition
{
    /// <summary>
    /// The well-known name for the closed-stream constraint.
    /// </summary>
    public static readonly ConstraintName WellKnownName = "closed-stream";

    /// <inheritdoc/>
    public ConstraintName Name => WellKnownName;

    /// <inheritdoc/>
    public bool Equals(IConstraintDefinition? other) => other is ClosedStreamConstraintDefinition;
}
