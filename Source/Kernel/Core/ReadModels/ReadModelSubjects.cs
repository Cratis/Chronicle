// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Converts the persisted per-property compliance subject map between the document representations used by the kernel.
/// </summary>
internal static class ReadModelSubjects
{
    /// <summary>
    /// Retains the contributing event's subject for each changed property in an encrypted projection fold.
    /// </summary>
    /// <param name="state">The folded state.</param>
    /// <param name="changes">The event's changes.</param>
    /// <param name="event">The contributing event.</param>
    public static void TrackChanges(ExpandoObject state, IEnumerable<Change> changes, AppendedEvent @event)
    {
        if (@event.Context.Subject?.IsSet != true && !@event.Context.EventSourceId.IsSpecified)
        {
            return;
        }

        var dictionary = (IDictionary<string, object?>)state;
        var subject = SubjectFor(@event);
        var defaultSubject = dictionary.TryGetValue(WellKnownProperties.Subject, out var storedSubject) && storedSubject is string { Length: > 0 } value
            ? value
            : subject;
        var subjects = dictionary.TryGetValue(WellKnownProperties.Subjects, out var storedSubjects) ? From(storedSubjects) : [];
        Track(changes, subject);
        dictionary[WellKnownProperties.Subject] = defaultSubject;
        if (subjects.Count > 0)
        {
            dictionary[WellKnownProperties.Subjects] = ToExpandoObject(subjects);
        }
        else
        {
            dictionary.Remove(WellKnownProperties.Subjects);
        }

        void Track(IEnumerable<Change> contributions, string owner)
        {
            foreach (var change in contributions)
            {
                switch (change)
                {
                    case PropertiesChanged<ExpandoObject> propertiesChanged:
                        foreach (var difference in propertiesChanged.Differences)
                        {
                            SetOwner(difference.PropertyPath, owner);
                        }
                        break;
                    case ChildAdded childAdded:
                        SetOwner(childAdded.ChildrenProperty, owner);
                        break;
                    case NestedCleared nestedCleared:
                        SetOwner(nestedCleared.NestedProperty, owner);
                        break;
                    case Joined joined:
                        Track(joined.Changes, owner);
                        break;
                    case ResolvedJoin resolvedJoin:
                        Track(resolvedJoin.Changes, resolvedJoin.Source is AppendedEvent source ? SubjectFor(source) : owner);
                        break;
                }
            }
        }

        void SetOwner(PropertyPath path, string owner)
        {
            if (path.Segments.FirstOrDefault()?.Value is not { } property || WellKnownProperties.All.Contains(property))
            {
                return;
            }

            if (owner == defaultSubject)
            {
                subjects.Remove(property);
            }
            else
            {
                subjects[property] = owner;
            }
        }
    }

    /// <summary>
    /// Carries authoritative subject lineage through a schema conversion for a projected read model.
    /// </summary>
    /// <param name="state">The state carrying lineage.</param>
    /// <param name="json">The schema-converted JSON.</param>
    /// <returns>The JSON with its subject lineage.</returns>
    public static JsonObject CopyToJson(ExpandoObject state, JsonObject json)
    {
        var dictionary = (IDictionary<string, object?>)state;
        foreach (var property in new[] { WellKnownProperties.Subject, WellKnownProperties.Subjects })
        {
            if (dictionary.TryGetValue(property, out var value))
            {
                json[property] = JsonSerializer.SerializeToNode(value);
            }
        }

        return json;
    }

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

    static string SubjectFor(AppendedEvent @event) => @event.Context.Subject?.IsSet == true ? @event.Context.Subject.Value : @event.Context.EventSourceId.Value;
}
