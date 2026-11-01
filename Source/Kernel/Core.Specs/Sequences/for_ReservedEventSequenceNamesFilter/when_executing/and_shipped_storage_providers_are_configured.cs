// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.when_executing;

public class and_shipped_storage_providers_are_configured : given.a_reserved_event_sequence_names_filter
{
    IReservedEventSequenceIds _mongo;
    IReservedEventSequenceIds _sql;
    IReservedEventSequenceIds _inMemory;
    CommandResult[] _results;

    async Task Because()
    {
        var mongo = await Check(builder => builder.WithMongoDB("mongodb://localhost:27017"));
        var sql = await Check(builder => builder.WithSql(new ChronicleOptions
        {
            Storage = new Configuration.Storage { ConnectionDetails = "Data Source=:memory:" }
        }));
        var inMemory = await Check(builder => builder.WithInMemory());
        _mongo = mongo.Policy;
        _sql = sql.Policy;
        _inMemory = inMemory.Policy;
        _results = [mongo.Result, sql.Result, inMemory.Result];
    }

    async Task<(IReservedEventSequenceIds Policy, CommandResult Result)> Check(Action<IChronicleBuilder> configure)
    {
        // Exercise the shipped registrations without starting a silo or connecting to a database.
        using var host = Host.CreateDefaultBuilder()
            .UseOrleans(silo =>
            {
                silo.UseLocalhostClustering();
                configure(new ChronicleBuilder(silo, silo.Services, new ConfigurationBuilder().Build()));
                silo.Services.AddSingleton(_storage);
            })
            .Build();
        var filter = new ReservedEventSequenceNamesFilter(host.Services);
        return (host.Services.GetRequiredService<IReservedEventSequenceIds>(), await filter.OnExecution(ContextFor(AppendTo("orders"))));
    }

    [Fact] void should_resolve_mongodb_policy() => _mongo.ShouldBeOfExactType<MongoDBReservedEventSequenceIds>();
    [Fact] void should_resolve_sql_policy() => _sql.ShouldBeOfExactType<SqlReservedEventSequenceIds>();
    [Fact] void should_resolve_in_memory_policy() => _inMemory.ShouldBeOfExactType<InMemoryReservedEventSequenceIds>();
    [Fact] void should_allow_normal_names_in_all_hosts() => _results.All(result => result is null).ShouldBeTrue();
    [Fact] void should_not_access_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
}
