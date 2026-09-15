// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents a scope that has established behavior patterns.
/// </summary>
/// <param name="Id">The identifier of the scope - typically the user the behavior belongs to.</param>
/// <param name="Name">The display name of the identity the scope belongs to.</param>
/// <param name="UserName">The username of the identity the scope belongs to.</param>
/// <remarks>
/// A read model of its own rather than a query returning bare strings: a query has to return the read model it is
/// declared on to get a route at all. The scope id is the subject Chronicle groups behavior by, which is exactly
/// what the identity store already resolves to a display name for the Identities list - joining against it here
/// gives the heatmap a name to show instead of a bare subject id.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Patterns)]
public record PatternScope(string Id, string Name, string UserName)
{
    /// <summary>
    /// Gets the scopes that have established patterns.
    /// </summary>
    /// <param name="eventStore">The event store to get scopes for.</param>
    /// <param name="namespace">The namespace to get scopes for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the mined patterns and identities.</param>
    /// <returns>The scopes that have established patterns.</returns>
    internal static async Task<IEnumerable<PatternScope>> AllPatternScopes(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IStorage storage)
    {
        var eventStoreNamespace = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var scopes = await eventStoreNamespace.Patterns.GetScopes();
        var identities = await eventStoreNamespace.Identities.GetAll();
        var identitiesBySubject = identities.ToDictionary(identity => identity.Subject);

        return
        [
            .. scopes
                .Select(scope => scope.Value)
                .Order(StringComparer.Ordinal)
                .Select(scope => identitiesBySubject.TryGetValue(scope, out var identity)
                    ? new PatternScope(scope, identity.Name, identity.UserName)
                    : new PatternScope(scope, scope, scope))
        ];
    }
}
