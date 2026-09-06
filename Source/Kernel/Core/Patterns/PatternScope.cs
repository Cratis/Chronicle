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
/// <remarks>
/// A read model of its own rather than a query returning bare strings: a query has to return the read model it is
/// declared on to get a route at all.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Patterns)]
public record PatternScope(string Id)
{
    /// <summary>
    /// Gets the scopes that have established patterns.
    /// </summary>
    /// <param name="eventStore">The event store to get scopes for.</param>
    /// <param name="namespace">The namespace to get scopes for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the mined patterns.</param>
    /// <returns>The scopes that have established patterns.</returns>
    internal static async Task<IEnumerable<PatternScope>> AllPatternScopes(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IStorage storage)
    {
        var scopes = await storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace)
            .Patterns
            .GetScopes();

        return [.. scopes.Select(scope => scope.Value).Order(StringComparer.Ordinal).Select(scope => new PatternScope(scope))];
    }
}
