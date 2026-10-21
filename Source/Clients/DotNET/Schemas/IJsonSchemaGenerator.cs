// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

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
    /// Generates a precise event schema using the owning store's event identities.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="eventTypes">The owning store's finalized event registry.</param>
    /// <returns>The generated schema.</returns>
    /// <exception cref="RegistryAwareEventTypeSchemasNotSupported">The generator does not support a required explicit compensation binding.</exception>
    JsonSchema Generate(Type type, IEventTypes eventTypes) => CompensationSchemaBinding.RequiresRegistry(type, eventTypes)
        ? throw new RegistryAwareEventTypeSchemasNotSupported(GetType(), type)
        : Generate(type);

    /// <summary>
    /// Generate a <see cref="JsonSchema"/> for a read model type.
    /// </summary>
    /// <param name="type">Read model <see cref="Type"/> to generate for.</param>
    /// <returns>A generated <see cref="JsonSchema"/>.</returns>
    /// <remarks>
    /// Uses the same precise shape as <see cref="Generate(Type)"/>.
    /// </remarks>
    JsonSchema GenerateForReadModel(Type type) => Generate(type);

    /// <summary>
    /// Generates the legacy event schema for a kernel without precise schema support.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <returns>The legacy schema.</returns>
    /// <exception cref="LegacyEventTypeSchemasNotSupported">The generator does not support legacy event schemas.</exception>
    JsonSchema GenerateLegacyEventType(Type type) => throw new LegacyEventTypeSchemasNotSupported();

    /// <summary>
    /// Generates a legacy event schema using the owning store's event identities.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="eventTypes">The owning store's finalized event registry.</param>
    /// <returns>The generated legacy schema.</returns>
    /// <exception cref="RegistryAwareEventTypeSchemasNotSupported">The generator does not support a required explicit compensation binding.</exception>
    JsonSchema GenerateLegacyEventType(Type type, IEventTypes eventTypes) => CompensationSchemaBinding.RequiresRegistry(type, eventTypes)
        ? throw new RegistryAwareEventTypeSchemasNotSupported(GetType(), type)
        : GenerateLegacyEventType(type);
}
