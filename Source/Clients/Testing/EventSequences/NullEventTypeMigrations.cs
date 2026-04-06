// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Dynamic;
using System.Text.Json.Nodes;
using KernelCore::Cratis.Chronicle.EventSequences.Migrations;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op implementation of <see cref="IEventTypeMigrations"/> for in-process testing.
/// </summary>
/// <remarks>
/// Returns the event content unchanged for the event's own generation, effectively applying no migration.
/// This is appropriate for testing scenarios where no event type migrations are defined.
/// </remarks>
internal sealed class NullEventTypeMigrations : IEventTypeMigrations
{
    /// <inheritdoc/>
    public Task<IDictionary<KernelEvents::EventTypeGeneration, ExpandoObject>> MigrateToAllGenerations(
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore,
        KernelEvents::EventType eventType,
        JsonObject content)
    {
        var expando = ConvertToExpandoObject(content);
        var result = new Dictionary<KernelEvents::EventTypeGeneration, ExpandoObject>
        {
            [eventType.Generation] = expando
        };

        return Task.FromResult<IDictionary<KernelEvents::EventTypeGeneration, ExpandoObject>>(result);
    }

    static ExpandoObject ConvertToExpandoObject(JsonObject obj)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;

        foreach (var (key, value) in obj)
        {
            dict[key] = ConvertNode(value);
        }

        return expando;
    }

    static object? ConvertNode(JsonNode? node) => node switch
    {
        JsonValue v => v.GetValue<object?>(),
        JsonObject o => ConvertToExpandoObject(o),
        JsonArray a => a.Select(ConvertNode).ToList(),
        null => null
    };
}
