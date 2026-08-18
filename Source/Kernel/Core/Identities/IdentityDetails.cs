// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Represents the read model for an identity, providing query access to the identity store.
/// </summary>
/// <param name="Subject">The identifier of the identity, referred to as subject.</param>
/// <param name="Name">The name of the identity.</param>
/// <param name="UserName">The username of the identity.</param>
/// <param name="OnBehalfOf">The identity this one acted on behalf of, when there is one.</param>
/// <remarks>
/// Named for what it carries rather than for the concept, because the chain it exposes through
/// <paramref name="OnBehalfOf"/> is the contract's identity and a read model called Identity would collide with it
/// in every generated client.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Identities)]
public record IdentityDetails(
    string Subject,
    string Name,
    string UserName,
    Contracts.Identities.Identity? OnBehalfOf)
{
    /// <summary>
    /// Gets all identities for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store to get identities for.</param>
    /// <param name="namespace">Namespace within the event store to get identities for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read identities from.</param>
    /// <returns>A collection of identities.</returns>
    internal static async Task<IEnumerable<IdentityDetails>> GetIdentities(string eventStore, string @namespace, IStorage storage)
    {
        var identities = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Identities.GetAll();
        return identities.ToDetails();
    }

    /// <summary>
    /// Observes all identities for the given event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store to observe identities for.</param>
    /// <param name="namespace">Namespace within the event store to observe identities for.</param>
    /// <param name="storage">The <see cref="IStorage"/> to observe identities from.</param>
    /// <returns>An observable subject emitting collections of identities.</returns>
    internal static ISubject<IEnumerable<IdentityDetails>> AllIdentities(string eventStore, string @namespace, IStorage storage) =>
        storage.GetEventStore(eventStore).GetNamespace(@namespace).Identities.ObserveAll().TransformSubject(_ => _.ToDetails());
}
