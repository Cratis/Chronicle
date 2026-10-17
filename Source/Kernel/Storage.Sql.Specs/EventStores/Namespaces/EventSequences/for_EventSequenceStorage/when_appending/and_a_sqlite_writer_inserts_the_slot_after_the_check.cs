// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending;

public class and_a_sqlite_writer_inserts_the_slot_after_the_check : given.a_storage_for_appended_generation
{
    DuplicateEventSequenceNumber _duplicate;
    Exception _error;

    void Establish()
    {
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>().UseSqlite(_connectionString);
        builder.AddConceptAsSupport();
        void CompetingWrite()
        {
            SeedEvent(4UL);
            SeedEvent(9UL);
        }
        builder.AddInterceptors(new given.CompetingSlotInsert(CompetingWrite));
        var options = builder.Options;
        _database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(new EventSequenceDbContext(options, _tableName, _migrator), () => { })));
    }

    async Task Because() => _error = await Catch.Exception(async () => (await Append(4UL)).TryGetError(out _duplicate));

    [given.sqlite.Fact] void should_not_throw_the_unique_violation() => _error.ShouldBeNull();
    [given.sqlite.Fact] void should_report_the_duplicate_slot() => _duplicate.ShouldNotBeNull();
    [given.sqlite.Fact] void should_use_the_committed_tails_next_number() => _duplicate.NextAvailableSequenceNumber.ShouldEqual((EventSequenceNumber)10UL);
    [given.sqlite.Fact] void should_preserve_the_competing_writers_slot() => Stored(4UL).SequenceNumber.ShouldEqual(4UL);
}
