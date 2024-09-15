// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;
using StorageIdentity = Cratis.Chronicle.Concepts.Identities.Identity;

namespace Cratis.Api.Identities;

/// <summary>
/// Represents the API for querying identities.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
[Route("/api/event-store/{eventStore}/{namespace}/identities")]
public class IdentitiesQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Gets all identities.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get identities for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get identities for.</param>
    /// <returns>Collection of identities.</returns>
    [HttpGet]
    public ISubject<IEnumerable<Identity>> AllIdentities(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        return new TransformingSubject<IEnumerable<StorageIdentity>, IEnumerable<Identity>>(
            namespaceStorage.Identities.ObserveAll(),
            _ => _.Select(i => new Identity(i.Subject, i.Name, i.UserName)));
    }
}
