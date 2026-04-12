// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using UniqueEventTypeConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueEventTypeConstraintDefinition;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Converts between <see cref="IConstraintDefinition"/> and <see cref="Constraint"/>.
/// </summary>
internal static class ConstraintConverters
{
    /// <summary>
    /// Convert a <see cref="IConstraintDefinition"/> to a <see cref="Constraint"/>.
    /// </summary>
    /// <param name="definition"><see cref="IConstraintDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="Constraint"/>.</returns>
    /// <exception cref="UnknownConstraintType">When an unknown constraint type is encountered.</exception>
    internal static Constraint ToContract(this IConstraintDefinition definition) => definition switch
    {
        UniqueEventTypeConstraintDefinition uniqueEventTypeConstraintDefinition => uniqueEventTypeConstraintDefinition.ToContract(),
        UniqueConstraintDefinition uniqueConstraintDefinition => uniqueConstraintDefinition.ToContract(),
        _ => throw new UnknownConstraintType(definition)
    };

    /// <summary>
    /// Convert a <see cref="UniqueEventTypeConstraintDefinition"/> to a <see cref="Constraint"/>.
    /// </summary>
    /// <param name="definition">Definition to convert.</param>
    /// <returns>Converted <see cref="Constraint"/>.</returns>
    internal static Constraint ToContract(this UniqueEventTypeConstraintDefinition definition) => new()
    {
        Name = definition.Name,
        Type = (Contracts.Events.Constraints.ConstraintType)ConstraintType.UniqueEventType,
        Definition = new(new UniqueEventTypeConstraintDefinitionContract
        {
            EventTypeId = definition.EventTypeId
        }),
        Scope = definition.Scope?.ToContract()
    };

    /// <summary>
    /// Convert a <see cref="UniqueConstraintDefinition"/> to a <see cref="Constraint"/>.
    /// </summary>
    /// <param name="definition">Definition to convert.</param>
    /// <returns>Converted <see cref="Constraint"/>.</returns>
    internal static Constraint ToContract(this UniqueConstraintDefinition definition) => new()
    {
        Name = definition.Name,
        Type = (Contracts.Events.Constraints.ConstraintType)ConstraintType.Unique,
        RemovedWith = definition.RemovedWith?.Value,
        Definition = new(new Contracts.Events.Constraints.UniqueConstraintDefinition
        {
            EventDefinitions = definition.EventsWithProperties.Select(_ => new Contracts.Events.Constraints.UniqueConstraintEventDefinition
            {
                EventTypeId = _.EventTypeId,
                Properties = _.Properties,
            }).ToList()
        }),
        Scope = definition.Scope?.ToContract()
    };

    /// <summary>
    /// Convert a client <see cref="ConstraintScope"/> to a contract <see cref="Contracts.Events.Constraints.ConstraintScope"/>.
    /// </summary>
    /// <param name="scope"><see cref="ConstraintScope"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Events.Constraints.ConstraintScope"/>.</returns>
    internal static Contracts.Events.Constraints.ConstraintScope ToContract(this ConstraintScope scope) => new()
    {
        EventSourceType = scope.EventSourceType?.Value,
        EventStreamType = scope.EventStreamType?.Value,
        EventStreamId = scope.EventStreamId?.Value
    };
}
