// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints;

/// <summary>
/// Converter methods for working with <see cref="IConstraintDefinition"/> converting to and from SQL representations.
/// </summary>
public static class ConstraintDefinitionConverters
{
    /// <summary>
    /// Convert to a <see cref="ConstraintDefinition">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="IConstraintDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="ConstraintDefinition"/>.</returns>
    public static ConstraintDefinition ToSql(this IConstraintDefinition definition)
    {
        var type = definition.GetType();
        var typeName = type.Name;
        var serialized = JsonSerializer.Serialize(definition, type);

        return new ConstraintDefinition
        {
            Name = definition.Name.Value,
            Type = typeName,
            Definition = serialized
        };
    }

    /// <summary>
    /// Convert to <see cref="IConstraintDefinition"/> from <see cref="ConstraintDefinition"/>.
    /// </summary>
    /// <param name="entity"><see cref="ConstraintDefinition"/> to convert from.</param>
    /// <returns>Converted <see cref="IConstraintDefinition"/>.</returns>
    /// <exception cref="UnknownConstraintType">Thrown when the constraint type is unknown.</exception>
    public static IConstraintDefinition ToKernel(this ConstraintDefinition entity)
    {
        var type = Type.GetType($"Cratis.Chronicle.Concepts.Events.Constraints.{entity.Type}, Cratis.Chronicle.Concepts") ??
                   throw new UnknownConstraintType(typeof(IConstraintDefinition));

        var deserialized = JsonSerializer.Deserialize(entity.Definition, type) as IConstraintDefinition;
        return deserialized ?? throw new UnknownConstraintType(type);
    }
}
