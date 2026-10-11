// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_enforcing_scoped_constraints;

/// <summary>
/// A fluent <see cref="IConstraint"/> allowing one <see cref="ScopedShiftStarted"/> per employee within each event stream.
/// </summary>
public class OneShiftPerEmployeeAndStream : IConstraint
{
    /// <summary>
    /// The name of the constraint.
    /// </summary>
    public const string Name = "OneShiftPerEmployeeAndStream";

    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder
            .PerEventStreamId()
            .Unique<ScopedShiftStarted>(name: Name);
}
