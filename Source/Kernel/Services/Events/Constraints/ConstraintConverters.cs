// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Services.Events.Constraints;

/// <summary>
/// Represents converter methods between <see cref="Contracts.Events.Constraints.Constraint"/> and <see cref="IConstraintDefinition"/>.
/// </summary>
public static class ConstraintConverters
{
    /// <summary>
    /// Convert <see cref="Contracts.Events.Constraints.Constraint"/> to <see cref="IConstraintDefinition"/>.
    /// </summary>
    /// <param name="constraint"><see cref="Contracts.Events.Constraints.Constraint"/> to convert from.</param>
    /// <returns>Collection of <see cref="IConstraintDefinition"/>.</returns>
    public static IConstraintDefinition ToChronicle(this Contracts.Events.Constraints.Constraint constraint)
    {
        return constraint.Type switch
        {
            Contracts.Events.Constraints.ConstraintType.Unique =>
                new UniqueConstraintDefinition(
                    constraint.Name,
                    constraint.Definition.Value0!.EventDefinitions.Select(e => e.ToChronicle()),
                    constraint.RemovedWith is null ? null : (EventTypeId)constraint.RemovedWith,
                    constraint.Definition.Value0!.IgnoreCasing),

            Contracts.Events.Constraints.ConstraintType.UniqueEventType =>
                new UniqueEventTypeConstraintDefinition(
                    constraint.Name,
                    constraint.Definition.Value1!.EventTypeId),

            _ => null!
        };
    }
}
