// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Extensions for <see cref="UniqueConstraintDefinition"/>.
/// </summary>
public static class UniqueConstraintDefinitionExtensions
{
    /// <summary>
    /// Check if the <see cref="UniqueConstraintDefinition"/> supports a given event type.
    /// </summary>
    /// <param name="definition"><see cref="UniqueConstraintDefinition"/> to check.</param>
    /// <param name="eventTypeId"><see cref="EventTypeId"/> to check for.</param>
    /// <returns>True if it does, false if not.</returns>
    public static bool SupportsEventType(this UniqueConstraintDefinition definition, EventTypeId eventTypeId) =>
        definition.EventDefinitions.Any(_ => _.EventTypeId == eventTypeId);

    /// <summary>
    /// Get the property and value for the <see cref="UniqueConstraintDefinition"/> based on the <see cref="ConstraintValidationContext"/>.
    /// </summary>
    /// <param name="definition"><see cref="UniqueConstraintDefinition"/> to get for.</param>
    /// <param name="context">The <see cref="ConstraintValidationContext"/> to get it relative to.</param>
    /// <returns>Tuple with property and value.</returns>
    public static IEnumerable<UniqueConstraintPropertyAndValue> GetPropertiesAndValues(this UniqueConstraintDefinition definition, ConstraintValidationContext context)
    {
        var contentAsDictionary = (context.Content as IDictionary<string, object>)!;
        var eventDefinition = definition.EventDefinitions.Single(_ => _.EventTypeId == context.EventTypeId);
        return eventDefinition.Properties
                .ToDictionary(_ => _, property => contentAsDictionary[property]?.ToString())
                .Where(kvp => kvp.Value != null)
                .Select(kvp => new UniqueConstraintPropertyAndValue(kvp.Key, kvp.Value!));
    }

    /// <summary>
    /// Get the unique value for the properties and values.
    /// </summary>
    /// <param name="propertiesWithValues">The <see cref="UniqueConstraintPropertyAndValue"/> to get for.</param>
    /// <returns>A string representing the unique value.</returns>
    public static string GetValue(this IEnumerable<UniqueConstraintPropertyAndValue> propertiesWithValues) =>
        string.Join('-', propertiesWithValues.Select(_ => _.Value));
}
