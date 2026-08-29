// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IPatternsService = Cratis.Chronicle.Contracts.Patterns.IPatterns;

namespace Cratis.Chronicle.Api.Patterns;

/// <summary>
/// Represents a scope that has established behavior patterns.
/// </summary>
/// <param name="Id">The identifier of the scope - typically the user the behavior belongs to.</param>
[ReadModel]
public record PatternScope(string Id)
{
    /// <summary>
    /// Get the scopes that have established patterns.
    /// </summary>
    /// <param name="patterns">The <see cref="IPatternsService"/> contract.</param>
    /// <param name="eventStore">The event store to get scopes for.</param>
    /// <param name="namespace">The namespace to get scopes for.</param>
    /// <returns>Collection of <see cref="PatternScope"/>.</returns>
    /// <remarks>
    /// A read model of its own rather than a query returning bare strings: a query has to return the read model it
    /// is declared on to get an HTTP route at all.
    /// </remarks>
    public static async Task<IEnumerable<PatternScope>> AllPatternScopes(
        IPatternsService patterns,
        string eventStore,
        string @namespace) =>
        [.. (await patterns.GetScopes(new()
        {
            EventStore = eventStore,
            Namespace = @namespace
        })).Select(scope => new PatternScope(scope))];
}
