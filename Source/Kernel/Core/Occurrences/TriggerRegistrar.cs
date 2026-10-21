// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Validates plaintext trigger contracts and preserves their schema revisions on replay.
/// </summary>
public class TriggerRegistrar
{
    /// <summary>
    /// Registers trigger contracts, validating the entire request before writing any definition.
    /// </summary>
    /// <param name="storage">The event-store definition storage.</param>
    /// <param name="registrations">The payload contracts.</param>
    /// <returns>The registration outcome.</returns>
    public async Task<TriggerRegistrationResult> Register(ITriggerDefinitionsStorage storage, IEnumerable<TriggerRegistration> registrations)
    {
        var prepared = new List<TriggerRegistration>();
        var errors = new List<string>();
        foreach (var registration in registrations)
        {
            if (string.IsNullOrWhiteSpace(registration.Id) || string.IsNullOrWhiteSpace(registration.Name))
            {
                errors.Add("TriggerIdentityInvalid");
                continue;
            }

            // Parse before the first write. Invalid schemas never become a stored ingress contract.
            var schema = await JsonSchema.FromJsonAsync(registration.Schema);
            if (CarriesCompliance(JsonNode.Parse(registration.Schema)))
            {
                errors.Add($"TriggerSchemaCarriesCompliance:{registration.Id}");
                continue;
            }

            prepared.Add(registration with { Schema = schema.ToJson() });
        }

        if (errors.Count > 0)
        {
            return new(false, errors);
        }

        foreach (var registration in prepared)
        {
            if (!await Save(storage, registration))
            {
                return new(false, [$"TriggerRegistrationConflicted:{registration.Id}"]);
            }
        }

        return new(true, []);
    }

    static bool CarriesCompliance(JsonNode? node) => node switch
    {
        JsonObject value => value.Any(property => (property.Key == "compliance" && property.Value is JsonArray { Count: > 0 }) || CarriesCompliance(property.Value)),
        JsonArray array => array.Any(CarriesCompliance),
        _ => false
    };

    static async Task<bool> Save(ITriggerDefinitionsStorage storage, TriggerRegistration registration)
    {
        var id = new TriggerId(registration.Id);

        // Every loser reloads the durable winner; an unknown write outcome propagates for caller retry.
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var existing = await storage.Get(id);
            if (existing is not null && existing.Name == registration.Name && existing.Schema == registration.Schema)
            {
                return true;
            }

            var revision = existing is null ? 1 : existing.Revision + 1;
            if (await storage.TrySave(new(id, registration.Name, registration.Schema, revision), existing?.Revision))
            {
                return true;
            }
        }

        return false;
    }
}
