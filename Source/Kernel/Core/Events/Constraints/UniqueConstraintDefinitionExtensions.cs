// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Properties;

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
        var eventDefinition = definition.EventDefinitions.Single(_ => _.EventTypeId == context.EventTypeId);
        return eventDefinition.Properties
                .ToDictionary(
                    _ => _,
                    property => new PropertyPath(property).GetValue(context.Content, ArrayIndexers.NoIndexers)?.ToString())
                .Where(kvp => kvp.Value != null)
                .Select(kvp => new UniqueConstraintPropertyAndValue(kvp.Key, kvp.Value!));
    }

    /// <summary>
    /// Get the unique value for the properties and values.
    /// </summary>
    /// <param name="propertiesWithValues">The <see cref="UniqueConstraintPropertyAndValue"/> to get for.</param>
    /// <param name="ignoreCasing">Whether to ignore casing when computing the hash.</param>
    /// <returns>A SHA-256 hash of the concatenated string representing the unique value.</returns>
    /// <remarks>
    /// The value is hashed using SHA-256 to avoid storing PII data in the constraint indexes.
    /// The hash is computed from the unencrypted concatenated property values.
    /// </remarks>
    public static string GetValue(this IEnumerable<UniqueConstraintPropertyAndValue> propertiesWithValues, bool ignoreCasing = false)
    {
        var concatenatedValue = string.Join('-', propertiesWithValues.Select(_ => _.Value));
        if (ignoreCasing)
        {
            concatenatedValue = concatenatedValue.ToLowerInvariant();
        }
        return ComputeSha256Hash(concatenatedValue);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the given input string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>The SHA-256 hash as a lowercase hexadecimal string.</returns>
    static string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
