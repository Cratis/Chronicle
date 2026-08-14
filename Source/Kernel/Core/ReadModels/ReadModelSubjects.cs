// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Converts the persisted per-property compliance subject map between the document representations used by the kernel.
/// </summary>
internal static class ReadModelSubjects
{
    /// <summary>
    /// Read a subject map from a stored document value.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <returns>The subject keyed by top-level read model property.</returns>
    public static Dictionary<string, string> From(object? value)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        switch (value)
        {
            case JsonObject jsonObject:
                foreach (var (property, subject) in jsonObject)
                {
                    if (subject?.GetValue<string>() is { Length: > 0 } subjectValue)
                    {
                        result[property] = subjectValue;
                    }
                }
                break;

            case JsonElement { ValueKind: JsonValueKind.Object } jsonElement:
                foreach (var property in jsonElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { Length: > 0 } subjectValue)
                    {
                        result[property.Name] = subjectValue;
                    }
                }
                break;

            case IDictionary<string, object?> dictionary:
                foreach (var (property, subject) in dictionary)
                {
                    if (subject?.ToString() is { Length: > 0 } subjectValue)
                    {
                        result[property] = subjectValue;
                    }
                }
                break;
        }

        return result;
    }

    /// <summary>
    /// Convert a subject map to the dynamic document representation used by projection state.
    /// </summary>
    /// <param name="subjects">The subjects keyed by property.</param>
    /// <returns>An <see cref="ExpandoObject"/> containing the subject map.</returns>
    public static ExpandoObject ToExpandoObject(IReadOnlyDictionary<string, string> subjects)
    {
        var result = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)result;
        foreach (var (property, subject) in subjects)
        {
            dictionary[property] = subject;
        }

        return result;
    }
}
