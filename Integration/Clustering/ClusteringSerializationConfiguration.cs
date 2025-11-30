// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Json;

namespace Cratis.Chronicle.Clustering.Integration;

/// <summary>
/// Shared serialization configuration for clustering tests.
/// </summary>
public static class ClusteringSerializationConfiguration
{
    /// <summary>
    /// Creates the JSON serializer options for clustering.
    /// </summary>
    /// <returns>Configured <see cref="JsonSerializerOptions"/>.</returns>
    public static JsonSerializerOptions CreateJsonSerializerOptions() =>
        new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new EnumConverterFactory(),
                new EnumerableConceptAsJsonConverterFactory(),
                new ConceptAsJsonConverterFactory(),
                new DateOnlyJsonConverter(),
                new TimeOnlyJsonConverter()
            }
        };

    /// <summary>
    /// Predicate for filtering types that should use JSON serialization.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type should use JSON serialization.</returns>
    public static bool IsSerializableType(Type type) => type.Namespace?.StartsWith("Cratis") ?? false;
}
