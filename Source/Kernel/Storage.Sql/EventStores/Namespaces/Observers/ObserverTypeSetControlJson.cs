// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Serializes the control as one JSON value, with event-type identifiers as object keys.
/// </summary>
internal static class ObserverTypeSetControlJson
{
    static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new ConceptAsJsonConverterFactory() }
    };

    /// <summary>
    /// Serializes initialized control without writing a JSON value for legacy state.
    /// </summary>
    /// <param name="control">The optional control.</param>
    /// <returns>The JSON value, or null before initialization.</returns>
    internal static string? Serialize(ObserverTypeSetControl? control)
    {
        if (control is null)
        {
            return null;
        }

        var stored = new StoredControl(
            control.Revision,
            control.Contract,
            control.Members.ToDictionary(pair => pair.Key.Value, pair => pair.Value),
            control.Registrations,
            control.RetirementResults,
            control.PendingChange);

        return JsonSerializer.Serialize(stored, _options);
    }

    /// <summary>
    /// Deserializes control without initializing legacy state.
    /// </summary>
    /// <param name="json">The optional stored JSON value.</param>
    /// <returns>The control, or null before initialization.</returns>
    internal static ObserverTypeSetControl? Deserialize(string? json)
    {
        if (json is null)
        {
            return null;
        }

        var stored = JsonSerializer.Deserialize<StoredControl>(json, _options)!;

        return new(
            stored.Revision,
            stored.Contract,
            stored.Members.ToDictionary(pair => new EventTypeId(pair.Key), pair => pair.Value),
            stored.Registrations)
        {
            RetirementResults = stored.RetirementResults,
            PendingChange = stored.PendingChange
        };
    }

    sealed record StoredControl(
        ObserverTypeSetRevision Revision,
        ObserverCompatibilityContract Contract,
        IReadOnlyDictionary<string, ObserverTypeSetMember> Members,
        IReadOnlyList<ObserverRegistrationIdentity> Registrations,
        IReadOnlyList<ObserverEventTypeRetirementResult>? RetirementResults,
        ObserverTypeSetChange? PendingChange);
}
