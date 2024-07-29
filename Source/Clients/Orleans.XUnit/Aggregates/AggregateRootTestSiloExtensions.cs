// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Orleans.TestKit;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents extension methods for working with testing with <see cref="AggregateRoot"/>.
/// </summary>
public static class AggregateRootTestSiloExtensions
{
    /// <summary>
    /// Create an instance of an <see cref="AggregateRoot"/> for testing.
    /// </summary>
    /// <typeparam name="TAggregateRoot">Type of <see cref="AggregateRoot"/> to create.</typeparam>
    /// <param name="silo"><see cref="TestKitSilo"/> to create the <see cref="AggregateRoot"/> in.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to create the <see cref="AggregateRoot"/> with.</param>
    /// <returns>An instance of the <see cref="AggregateRoot"/> created.</returns>
    public static async Task<TAggregateRoot> Create<TAggregateRoot>(this TestKitSilo silo, EventSourceId eventSourceId)
        where TAggregateRoot : IAggregateRoot
    {
        var aggregateRoot = await silo.CreateGrainAsync<TAggregateRoot>(eventSourceId);
        return aggregateRoot;
    }
}
