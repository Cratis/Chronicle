// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_no_sequence_number_is_occupied : given.an_event_sequence_storage_recording_commands
{
    Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber> _result;
    int _storedCount;

    async Task Because()
    {
        _result = await AppendMany(Enumerable.Range(1, 1000).Select(number => new EventSequenceNumber((ulong)number)).ToArray());
        await using var context = CreateContext();
        _storedCount = await context.Events.CountAsync();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_every_event() => _result.AsT0.Count().ShouldEqual(1000);
    [Fact] void should_persist_every_event() => _storedCount.ShouldEqual(1000);
    [Fact] void should_check_occupancy_once() => _commands.Commands.Count(command => command.Contains("EXISTS", StringComparison.OrdinalIgnoreCase)).ShouldEqual(1);
}
