// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Identities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventCursor.given;

/// <summary>
/// Sets up an <see cref="EventCursor"/> over a context whose asynchronous disposal suspends before completing,
/// which is what a network-backed provider does when it closes its connection. SQLite completes its disposal
/// inline and would hide the difference between disposing the scope and blocking a thread on its disposal.
/// </summary>
public class a_cursor_over_a_context_whose_async_disposal_suspends : Specification
{
    protected SqliteConnection _connection;
    protected EventCursor _cursor;

    void Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<EventSequenceDbContext>()
            .UseSqlite(_connection)
            .AddConceptAsSupport()
            .Options;

        var context = new suspending_event_sequence_db_context(options);
        context.Database.EnsureCreated();

        _cursor = new EventCursor(
            context.Events,
            new DbContextScope<EventSequenceDbContext>(context, () => { }),
            "test-store",
            "test-namespace",
            Substitute.For<IIdentityStorage>());
    }

    void Destroy() => _connection.Dispose();

    sealed class suspending_event_sequence_db_context(DbContextOptions<EventSequenceDbContext> options)
        : EventSequenceDbContext(options, "event-sequence", Substitute.For<IEventSequenceMigrator>())
    {
        public override async ValueTask DisposeAsync()
        {
            await Task.Yield();
            await base.DisposeAsync();
        }
    }
}
