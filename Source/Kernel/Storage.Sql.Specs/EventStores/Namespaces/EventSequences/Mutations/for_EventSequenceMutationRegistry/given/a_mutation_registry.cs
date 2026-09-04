// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry.given;

/// <summary>
/// Sets up an <see cref="EventSequenceMutationRegistry"/> backed by a shared in-memory SQLite database.
/// A single open connection is shared across every <see cref="IDatabase.Namespace"/> scope so rows
/// written by one call survive into the next - the registry opens a fresh <see cref="NamespaceDbContext"/>
/// per method call, exactly as it does in production.
/// </summary>
public class a_mutation_registry : Specification, IDisposable
{
    protected static readonly EventStoreName _eventStore = "test-store";
    protected static readonly EventStoreNamespaceName _namespace = "test-namespace";

    protected SqliteConnection _connection;
    protected IDatabase _database;
    protected IEventSequenceMutationRegistry _registry;
    protected EventSequenceMutationIdentity _target;
    protected EventSequenceMutationTarget _proposedTarget;
    protected EventSequenceMutationRequest _request;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var schemaContext = CreateContext())
        {
            schemaContext.Database.EnsureCreated();
        }

        _database = Substitute.For<IDatabase>();
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(CreateContext(), () => { })));

        _registry = new EventSequenceMutationRegistry(_eventStore, _namespace, _database);
        _target = Identity("target-sequence");
        _proposedTarget = new(10UL, 13UL, 3UL);
        _request = Request(_target);
    }

    protected static NamespaceDbContext CreateContextFor(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseSqlite(connection)
            .AddConceptAsSupport()
            .Options;
        return new NamespaceDbContext(options);
    }

    protected NamespaceDbContext CreateContext() => CreateContextFor(_connection);

    protected static EventSequenceMutationIdentity Identity(string display) => EventSequenceMutationIdentity.TryCreate(display).Identity!;

    protected static EventSequenceMutationRequest Request(
        EventSequenceMutationIdentity target,
        ulong originSequenceNumber = 42,
        string payload = "{\"name\":\"Ada\"}",
        string hash = "command-hash")
    {
        var origin = Identity("origin-sequence");
        const EventSequenceMutationKind kind = EventSequenceMutationKind.Revision;
        var id = EventSequenceMutationDigestCalculator.CalculateId(target, origin, originSequenceNumber, kind);
        return new(id, target, new(origin, originSequenceNumber), kind, new(payload, hash));
    }

    protected static async Task<EventSequenceMutationRegistryTransitionResult> Apply(
        IEventSequenceMutationRegistry registry,
        EventSequenceMutationBeginResult begin,
        EventSequenceMutationTransition transition) =>
        await registry.Transition(begin.Active!.TargetSequence, begin.Token!, transition);

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
