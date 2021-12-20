// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Schemas
{
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
    }
}
