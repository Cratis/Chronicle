// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_another_writer_revises_after_the_read : given.an_event_sequence_storage
{
    given.CompetingRevision _interceptor;
    AppendedEvent _read;
    EventEntry _stored;

    async Task Establish()
    {
        await Append(EventSequenceNumber.First, Content("original"), new EventHash("original-hash"));

        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>().UseSqlite(_connectionString);
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        _interceptor = new given.CompetingRevision(() => _storage.Revise(EventSequenceNumber.First, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, Content("competitor"), new EventHash("competitor-hash")));
        builder.AddInterceptors(_interceptor);
        var options = builder.Options;
        _database.EventSequenceTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>())
            .Returns(_ => Task.FromResult(new DbContextScope<EventSequenceDbContext>(new EventSequenceDbContext(options, _tableName, _migrator), () => { })));
    }

    async Task Because()
    {
        await _storage.Revise(EventSequenceNumber.First, _eventType, CorrelationId.New(), [], [], DateTimeOffset.UtcNow, Content("later"), new EventHash("later-hash"));
        _read = await _storage.GetEventAt(EventSequenceNumber.First);
        await using var context = CreateContext();
        _stored = context.Events.AsNoTracking().Single(_ => _.SequenceNumber == 0);
    }

    [given.sqlite.Fact] void should_retry_after_losing_the_compare_and_swap() => _interceptor.UpdatesExecuted.ShouldBeGreaterThan(2);
    [given.sqlite.Fact] void should_keep_both_revisions() => EventEntryConverter.GetRevisions(_stored).Count.ShouldEqual(2);
    [given.sqlite.Fact] void should_keep_the_competitor_first() => EventEntryConverter.GetRevisions(_stored)[0].Hash.ShouldEqual("competitor-hash");
    [given.sqlite.Fact] void should_return_the_later_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("later");

    static ExpandoObject Content(string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["value"] = value;
        return content;
    }
}
