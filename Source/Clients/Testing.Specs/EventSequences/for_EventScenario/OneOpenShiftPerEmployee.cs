// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A fluent <see cref="IConstraint"/> allowing at most one open <see cref="ShiftStarted"/> per employee, released by
/// <see cref="ShiftEnded"/>.
/// </summary>
public class OneOpenShiftPerEmployee : IConstraint
{
    /// <summary>
    /// The name of the constraint.
    /// </summary>
    public const string Name = "OneOpenShiftPerEmployee";

    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder
            .Unique<ShiftStarted>(name: Name)
            .RemovedWith<ShiftEnded>();
}
