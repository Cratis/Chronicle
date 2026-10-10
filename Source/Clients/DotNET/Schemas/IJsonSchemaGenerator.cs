// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Defines a generator that can generate <see cref="JsonSchema"/>.
/// </summary>
public interface IJsonSchemaGenerator
{
    /// <summary>
    /// Generate a <see cref="JsonSchema"/> for a specific type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to generate for.</param>
    /// <returns>A generated <see cref="JsonSchema"/>.</returns>
    JsonSchema Generate(Type type);

    /// <summary>
    /// Generate a <see cref="JsonSchema"/> for a read model type.
    /// </summary>
    /// <param name="type">Read model <see cref="Type"/> to generate for.</param>
    /// <returns>A generated <see cref="JsonSchema"/>.</returns>
    /// <remarks>
    /// Uses the same precise shape as <see cref="Generate"/>.
    /// </remarks>
    JsonSchema GenerateForReadModel(Type type) => Generate(type);

    /// <summary>
    /// Generates the legacy event schema for a kernel without precise schema support.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <returns>The legacy schema.</returns>
    /// <exception cref="LegacyEventTypeSchemasNotSupported">The generator does not support legacy event schemas.</exception>
    JsonSchema GenerateLegacyEventType(Type type) => throw new LegacyEventTypeSchemasNotSupported();
}
