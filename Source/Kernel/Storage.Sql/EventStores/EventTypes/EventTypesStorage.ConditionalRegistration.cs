// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes;

public partial class EventTypesStorage
{
    /// <inheritdoc/>
    public async Task<EventTypeRegistrationResult> RegisterIfAbsent(EventTypeToRegister eventType)
    {
        await using var scope = await database.EventStore(eventStore);
        var context = scope.DbContext;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var existing = await context.EventTypes.AsNoTracking().FirstOrDefaultAsync(_ => _.Id == eventType.Definition.Id);
            var result = EventTypeRegistrationResult.Compare(eventType.Definition, existing?.ToDefinition().Generations ?? []);
            if (result.Conflict is not null)
            {
                return result;
            }

            var registration = ConditionalRegistration(eventType, existing, result.AddedGenerations);
            if (Unchanged(existing, registration, result))
            {
                return result;
            }

            if (await TrySaveRegistration(context, existing, registration))
            {
                return result with { Changed = true };
            }
        }

        throw new EventTypeRegistrationContended(eventType.Definition.Id);
    }

    static bool Unchanged(EventType? existing, EventType registration, EventTypeRegistrationResult result) =>
        existing is not null && HasSameMetadata(existing, registration) &&
        existing.MigrationsJson == registration.MigrationsJson && result.AddedGenerations.Count == 0;

    static EventType ConditionalRegistration(EventTypeToRegister incoming, EventType? existing, IReadOnlyList<EventTypeGeneration> added)
    {
        var definition = incoming.Definition with
        {
            Generations = incoming.Definition.Generations.Where(_ => added.Contains(_.Generation)).ToList(),
            Migrations = []
        };
        var registration = definition.ToSql();
        registration.Source = incoming.Source;
        registration.Visibility = incoming.Visibility == EventTypeVisibility.Unspecified && existing is not null
            ? existing.Visibility
            : incoming.Visibility;
        registration.Origin = incoming.Origin;
        registration.MigrationsJson = PrepareMigrations(existing, incoming.Definition.Migrations);
        registration.MigrationVersionsJson = existing?.MigrationVersionsJson ?? "{}";
        registration.Schemas = MergeSchemas(existing?.Schemas ?? new Dictionary<uint, string>(), registration.Schemas);
        return registration;
    }
}
