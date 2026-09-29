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
    /// Use this for read models and <see cref="Generate"/> for event types. A read model schema can describe a property
    /// more completely than an event type schema may, since an event type's schema cannot change within a generation.
    /// The default implementation returns <see cref="Generate"/>.
    /// </remarks>
    JsonSchema GenerateForReadModel(Type type) => Generate(type);
}
