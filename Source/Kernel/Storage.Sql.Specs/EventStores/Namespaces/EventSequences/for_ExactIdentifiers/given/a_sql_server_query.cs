// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers.given;

/// <summary>
/// Translates identifier predicates without connecting to SQL Server.
/// </summary>
public class a_sql_server_query : Specification
{
    protected static string[] Translate(string spelling)
    {
        using var events = new EventSequenceDbContext(Options<EventSequenceDbContext>(), "events", Substitute.For<IEventSequenceMigrator>());
        using var partitions = new NamespaceDbContext(Options<NamespaceDbContext>());
        return
        [
            events.Events.WhereEventSource(events, spelling).ToQueryString(),
            events.Events.WhereSourceType(events, spelling).ToQueryString(),
            events.Events.WhereStreamType(events, spelling).ToQueryString(),
            events.Events.WhereStreamId(events, spelling).ToQueryString(),
            partitions.InFlightEvents.WherePartition(partitions, spelling).ToQueryString(),
            partitions.InFlightEvents.WhereExact(partitions, _ => _.ObserverId, spelling).ToQueryString()
        ];
    }

    static DbContextOptions<TContext> Options<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer("Server=.;Database=x")
            .AddConceptAsSupport()
            .WithoutServiceProviderCaching()
            .Options;
}
