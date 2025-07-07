// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Defines a generator that can generate <see cref="IJsonSchemaDocument"/>.
/// </summary>
public interface IJsonSchemaGenerator
{
    /// <summary>
    /// Generate a <see cref="IJsonSchemaDocument"/> for a specific type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to generate for.</param>
    /// <returns>A generated <see cref="IJsonSchemaDocument"/>.</returns>
    IJsonSchemaDocument Generate(Type type);
}
