// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Identifies the definition for which a replay is recommended.
/// </summary>
internal static class DefinitionChangeIdentity
{
    static readonly JsonSerializerOptions _options = new(Globals.JsonSerializerOptions)
    {
        Converters =
        {
            new ComplexKeyDictionaryJsonConverterFactory(),
            new ReadOnlyComplexKeyDictionaryJsonConverterFactory(),
            new PropertyPathJsonConverter()
        }
    };

    /// <summary>
    /// Computes a lowercase SHA-256 identity from the serialized definition.
    /// </summary>
    /// <param name="definition">The current observer definition.</param>
    /// <returns>The definition's change identity.</returns>
    internal static string For(object definition) =>
        Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(definition, _options)));
}
