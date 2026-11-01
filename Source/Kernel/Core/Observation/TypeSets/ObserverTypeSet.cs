// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents the durable type-set and initialization state of an observer.
/// </summary>
/// <param name="Revision">The committed membership revision.</param>
/// <param name="Members">The members with their incarnations and join fences.</param>
/// <param name="Registrations">The stored registration capabilities, not proof of connectivity.</param>
/// <param name="PendingChange">The pending membership intent, if any.</param>
/// <param name="RetirementResults">The retained final retirement results.</param>
/// <param name="InitializationState">The observer's initialization state.</param>
/// <param name="RefusalReasons">The latest initialization refusal reasons.</param>
[ReadModel]
[BelongsTo(WellKnownServices.ObserverTypeSets)]
public record ObserverTypeSet(
    long Revision,
    IEnumerable<ObserverTypeSetMember> Members,
    IEnumerable<ObserverTypeSetRegistration> Registrations,
    ObserverTypeSetChange? PendingChange,
    IEnumerable<ObserverEventTypeRetirement> RetirementResults,
    ObserverInitializationState InitializationState,
    IEnumerable<string> RefusalReasons)
{
    /// <summary>
    /// Gets the stored type-set state for an observer.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="storage">The observer storage.</param>
    /// <returns>The stored state, or an uninitialized empty type set.</returns>
    public static async Task<ObserverTypeSet> GetForObserver(string eventStore, string @namespace, string observerId, IStorage storage)
    {
        var state = await storage.GetEventStore(new(eventStore)).GetNamespace(new(@namespace)).Observers.GetReadOnly(new(observerId));
        return ObserverTypeSetStorageConverters.ToReadModel(state);
    }
}
