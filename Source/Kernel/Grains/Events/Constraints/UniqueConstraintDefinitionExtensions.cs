// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Extensions for <see cref="UniqueConstraintDefinition"/>.
/// </summary>
public static class UniqueConstraintDefinitionExtensions
{
    /// <summary>
    /// Get the property and value for the <see cref="UniqueConstraintDefinition"/> based on the <see cref="ConstraintValidationContext"/>.
    /// </summary>
    /// <param name="definition"><see cref="UniqueConstraintDefinition"/> to get for.</param>
    /// <param name="context">The <see cref="ConstraintValidationContext"/> to get it relative to.</param>
    /// <returns>Tuple with property and value.</returns>
    public static (string Property, string? Value) GetPropertyAndValue(this UniqueConstraintDefinition definition, ConstraintValidationContext context)
    {
        var property = definition.EventDefinitions.Single(_ => _.EventTypeId == context.EventTypeId).Property;
        var contentAsDictionary = (context.Content as IDictionary<string, object>)!;
        var value = contentAsDictionary[property]?.ToString();

        return (property, value);
    }
}
