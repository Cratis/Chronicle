// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_enforcing_scoped_constraints;

/// <summary>
/// A fluent <see cref="IConstraint"/> making <see cref="ScopedBadgeIssued.BadgeNumber"/> unique within each event stream.
/// </summary>
public class UniqueBadgeNumberPerStream : IConstraint
{
    /// <summary>
    /// The name of the constraint.
    /// </summary>
    public const string Name = "UniqueBadgeNumberPerStream";

    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder
            .PerEventStreamId()
            .Unique(_ => _
                .On<ScopedBadgeIssued>(e => e.BadgeNumber)
                .WithName(Name));
}
