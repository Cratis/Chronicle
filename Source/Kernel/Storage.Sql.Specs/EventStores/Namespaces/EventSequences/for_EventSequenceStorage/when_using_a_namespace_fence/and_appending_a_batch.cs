// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_using_a_namespace_fence;

public class and_appending_a_batch : given.a_fenced_storage
{
    AppendedEvent _appended;
    EventEntry _stored;
    NamedTagEntry[] _tags;

    async Task Because()
    {
        _appended = (await _fenced.AppendMany(_context, [_event])).AsT0.Single();
        await using var scope = await _database.EventSequenceTable(_eventStore, _namespace, _tableName);
        _stored = await scope.DbContext.Events.SingleAsync();
        _tags = await scope.DbContext.NamedTags.ToArrayAsync();
    }

    [Fact] void should_preserve_compensation() => _stored.CompensationFor.ShouldEqual(3UL);
    [Fact] void should_preserve_named_tags() => _tags.Length.ShouldEqual(1);
    [Fact] void should_not_persist_ordinary_append_identities() => _stored.PublicationId.ShouldBeNull();
    [Fact] void should_return_the_appended_generation() => _appended.Context.AppendedGeneration.ShouldEqual(_event.EventType.Generation);
    [Fact] void should_return_the_truncated_time() => _appended.Context.Occurred.ShouldEqual(EventSequenceStorage.TruncateToMicrosecond(_event.Occurred));
    [Fact] void should_return_compensation() => _appended.Context.CompensationFor.ShouldEqual(new EventSequenceNumber(3));
}
