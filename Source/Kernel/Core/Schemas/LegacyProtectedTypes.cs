// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>Per-walk, bounded kind checks for envelopes that did not record a JSON kind.</summary>
internal sealed class LegacyProtectedTypes
{
    const int MaxSchemas = 512;
    readonly Dictionary<JsonSchema, JsonObjectType> _types = new(ReferenceEqualityComparer.Instance);

    /// <summary>Checks whether a legacy schema proves one supported, non-null kind.</summary>
    /// <param name="schema">The protected boundary.</param>
    /// <returns>Whether its plaintext can be restored without choosing a union branch.</returns>
    internal bool IsSupported(JsonSchema schema) =>
        For(schema) is JsonObjectType.String or JsonObjectType.Integer or JsonObjectType.Number or JsonObjectType.Boolean or JsonObjectType.Array or JsonObjectType.Object;

    /// <summary>Gets the possible legacy kinds without selecting a contextual binding or a union branch.</summary>
    /// <param name="schema">The protected boundary.</param>
    /// <returns>Its combined kinds, or none if the bounded check cannot prove a kind.</returns>
    internal JsonObjectType For(JsonSchema schema)
    {
        if (!_types.TryGetValue(schema, out var type))
        {
            type = Inspect(schema);
            _types.Add(schema, type);
        }

        return type;
    }

    static JsonObjectType Inspect(JsonSchema schema)
    {
        var seen = new HashSet<JsonSchema>(ReferenceEqualityComparer.Instance);
        var active = new HashSet<JsonSchema>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(JsonSchema Schema, bool Exit)>();
        pending.Push((schema, false));
        var type = JsonObjectType.None;
        while (pending.TryPop(out var entry))
        {
            var current = entry.Schema;
            if (entry.Exit)
            {
                active.Remove(current);
                continue;
            }

            if (active.Contains(current)) return JsonObjectType.None;
            if (!seen.Add(current)) continue;
            if (seen.Count > MaxSchemas || current.MetadataReferences.Indeterminate) return JsonObjectType.None;
            type |= current.Type;
            active.Add(current);
            pending.Push((current, true));
            if (!ScheduleChildren(current, pending, seen.Count)) return JsonObjectType.None;
        }

        return type & ~JsonObjectType.Null;
    }

    static bool ScheduleChildren(JsonSchema current, Stack<(JsonSchema Schema, bool Exit)> pending, int visited)
    {
        var children = current.AllOf.Concat(current.AnyOf).Concat(current.OneOf).ToList();
        if (current.HasReference && current.Reference is { } reference) children.Add(reference);
        if (current.Type == JsonObjectType.None && children.Count == 0) return false;
        foreach (var child in children)
        {
            if (pending.Count + visited >= MaxSchemas) return false;
            pending.Push((child, false));
        }

        return true;
    }
}
