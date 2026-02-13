// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventHashCalculator"/>.
/// </summary>
[Singleton]
public class EventHashCalculator : IEventHashCalculator
{
    static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <inheritdoc/>
    public EventHash Calculate(EventTypeId eventTypeId, EventSourceId eventSourceId, ExpandoObject content)
    {
        var combinedData = $"{eventTypeId.Value}|{eventSourceId.Value}|{GetCanonicalJson(content)}";
        var bytes = Encoding.UTF8.GetBytes(combinedData);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    static string GetCanonicalJson(ExpandoObject obj)
    {
        var json = JsonSerializer.Serialize(obj, _jsonSerializerOptions);
        var jsonNode = JsonNode.Parse(json);
        var sortedNode = SortJsonNode(jsonNode);
        return JsonSerializer.Serialize(sortedNode, _jsonSerializerOptions);
    }

    static JsonNode? SortJsonNode(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            var sorted = new JsonObject();
            foreach (var property in jsonObject.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                sorted[property.Key] = SortJsonNode(property.Value);
            }
            return sorted;
        }

        if (node is JsonArray jsonArray)
        {
            var sorted = new JsonArray();
            foreach (var item in jsonArray)
            {
                sorted.Add(SortJsonNode(item));
            }
            return sorted;
        }

        return node?.DeepClone();
    }
}
