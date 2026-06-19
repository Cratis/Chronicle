// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Detects added, modified and removed read model instances by diffing successive materialized windows.
/// </summary>
/// <remarks>
/// The materialized API delivers a full window of instances on every change. Because the deserialized read
/// model no longer carries the last handled event sequence number, change type is determined by comparing the
/// serialized value of each instance — keyed by its <c>id</c> property — against the previous window.
/// </remarks>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> used to serialize instances for comparison.</param>
public class MaterializedReadModelDiffer(JsonSerializerOptions jsonSerializerOptions)
{
    static readonly string[] _keyPropertyNames = ["id", "_id", "Id"];

    readonly Dictionary<string, string> _previous = new(StringComparer.Ordinal);

    /// <summary>
    /// Diff the given window of instances against the previous window.
    /// </summary>
    /// <param name="window">The current window of read model instances.</param>
    /// <returns>The changes between the previous window and this one.</returns>
    public IReadOnlyList<MaterializedReadModelChange> Diff(IEnumerable<object> window)
    {
        var current = new Dictionary<string, (object Instance, string Json)>(StringComparer.Ordinal);
        foreach (var instance in window)
        {
            if (instance is null)
            {
                continue;
            }

            var json = JsonSerializer.Serialize(instance, jsonSerializerOptions);
            var key = ExtractKey(json);
            if (key is not null)
            {
                current[key] = (instance, json);
            }
        }

        var changes = new List<MaterializedReadModelChange>();

        foreach (var (key, (instance, json)) in current)
        {
            if (!_previous.TryGetValue(key, out var previousJson))
            {
                changes.Add(new MaterializedReadModelChange(key, instance, ReadModelChangeType.Added));
            }
            else if (!string.Equals(previousJson, json, StringComparison.Ordinal))
            {
                changes.Add(new MaterializedReadModelChange(key, instance, ReadModelChangeType.Modified));
            }
        }

        foreach (var removedKey in _previous.Keys.Where(_ => !current.ContainsKey(_)).ToArray())
        {
            changes.Add(new MaterializedReadModelChange(removedKey, null, ReadModelChangeType.Removed));
        }

        _previous.Clear();
        foreach (var (key, value) in current)
        {
            _previous[key] = value.Json;
        }

        return changes;
    }

    static string? ExtractKey(string json)
    {
        var node = JsonNode.Parse(json);
        if (node is not JsonObject obj)
        {
            return null;
        }

        foreach (var name in _keyPropertyNames)
        {
            if (obj.TryGetPropertyValue(name, out var value) && value is not null)
            {
                return value.ToString();
            }
        }

        return null;
    }
}
