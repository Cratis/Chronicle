// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Projections.Json;

/// <summary>
/// Represents an implementation of <see cref="IJsonProjectionChangesetSerializer"/>.
/// </summary>
public class JsonProjectionChangesetSerializer : IJsonProjectionChangesetSerializer
{
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonProjectionChangesetSerializer"/>.
    /// </summary>
    public JsonProjectionChangesetSerializer()
    {
        _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ConceptAsJsonConverterFactory(),
                new DateOnlyJsonConverter(),
                new TimeOnlyJsonConverter()
            }
        };
    }

    /// <inheritdoc/>
    public JsonNode Serialize<TSource, TTarget>(IChangeset<TSource, TTarget> changeset) => JsonSerializer.SerializeToNode(changeset, _serializerOptions)!;
}