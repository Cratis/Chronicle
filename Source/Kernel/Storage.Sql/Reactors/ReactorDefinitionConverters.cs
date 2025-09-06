// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Reactors;

/// <summary>
/// Converter methods for working with <see cref="Concepts.Observation.Reactors.ReactorDefinition"/> converting to and from SQL representations.
/// </summary>
public static class ReactorDefinitionConverters
{
    /// <summary>
    /// Convert to a <see cref="ReactorDefinition">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="Concepts.Observation.Reactors.ReactorDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="ReactorDefinition"/>.</returns>
    public static ReactorDefinition ToSql(this Concepts.Observation.Reactors.ReactorDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.ToArray(),
            IsReplayable = definition.IsReplayable,
        };

    /// <summary>
    /// Convert to <see cref="Concepts.Observation.Reactors.ReactorDefinition"/> from <see cref="ReactorDefinition"/>.
    /// </summary>
    /// <param name="schema"><see cref="ReactorDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="Concepts.Observation.Reactors.ReactorDefinition"/>.</returns>
    public static Concepts.Observation.Reactors.ReactorDefinition ToKernel(this ReactorDefinition schema) =>
        new(
            schema.Id,
            schema.Owner,
            schema.EventSequenceId,
            schema.EventTypes.ToArray(),
            schema.IsReplayable);
}
