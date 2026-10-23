// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
using Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_NativeUpserts.given;

public static class round_trip
{
    public static async Task<(bool Json, bool Converted, bool Composite, bool Null, int Occurrences)> Run(IDatabase database)
    {
        var applicationId = Guid.NewGuid();
        var started = DateTimeOffset.Parse("2026-01-01T12:00:00Z");
        EventType[] eventTypes = [new("native-event", EventTypeGeneration.First)];
        await using (var cluster = await database.Cluster())
        {
            await cluster.DbContext.Applications.Upsert(new ApplicationEntity { Id = applicationId, ClientId = "native-app", ClientSecret = "old" });
            await cluster.DbContext.Applications.Upsert(new ApplicationEntity
            {
                Id = applicationId, ClientId = "native-app", Permissions = ["read", "write"], RedirectUris = ["https://localhost/callback"]
            });
        }
        await using (var store = await database.EventStore("native-upserts"))
        {
            await store.DbContext.Reactors.Upsert(new EventStores.Reactors.ReactorDefinition { Id = "converted-reactor", Owner = ReactorOwner.Client });
            await store.DbContext.EventStoreSubscriptions.Upsert(new EventStoreSubscriptionDefinition { Id = "subscription" });
            await store.DbContext.EventStoreSubscriptions.Upsert(new EventStoreSubscriptionDefinition
            {
                Id = "subscription", SourceEventStore = "source", EventTypes = eventTypes
            });
        }
        await using (var ns = await database.Namespace("native-upserts", "namespace"))
        {
            await ns.DbContext.ReplayedModels.Upsert(new ReplayedModelOccurrence { ObserverId = "observer", Started = started, RevertModelName = "old" });
            await ns.DbContext.ReplayedModels.Upsert(new ReplayedModelOccurrence { ObserverId = "observer", Started = started, RevertModelName = "updated" });
            await ns.DbContext.ReplayedModels.Upsert(new ReplayedModelOccurrence { ObserverId = "observer", Started = started.AddHours(1), RevertModelName = "second" });
        }

        // All writes above must be visible from fresh contexts, without SaveChanges.
        await using var savedCluster = await database.Cluster();
        await using var savedStore = await database.EventStore("native-upserts");
        await using var savedNamespace = await database.Namespace("native-upserts", "namespace");
        var application = await savedCluster.DbContext.Applications.SingleAsync();
        var subscription = await savedStore.DbContext.EventStoreSubscriptions.SingleAsync();
        var reactor = await savedStore.DbContext.Reactors.SingleAsync();
        var occurrences = await savedNamespace.DbContext.ReplayedModels.ToListAsync();
        return (application.Permissions.SequenceEqual(["read", "write"]) && application.RedirectUris.SequenceEqual(["https://localhost/callback"]),
            reactor.Id.Value == "converted-reactor" && subscription.EventTypes.SequenceEqual(eventTypes) && subscription.SourceEventStore == "source",
            occurrences.Exists(row => row.Started == started && row.RevertModelName == "updated") &&
                occurrences.Exists(row => row.Started == started.AddHours(1) && row.RevertModelName == "second"),
            application.ClientSecret is null, occurrences.Count);
    }
}
