// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_the_batch_is_larger_than_one_chunk : given.an_event_sequence_storage_recording_commands
{
    Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber> _result;
    int _storedCount;

    void Establish() => SeedEvent(new EventSequenceNumber(1001));

    async Task Because()
    {
        _result = await AppendMany(Enumerable.Range(1, 1001).Select(number => new EventSequenceNumber((ulong)number)).ToArray());
        await using var context = CreateContext();
        _storedCount = await context.Events.CountAsync();
    }

    [Fact] void should_fail_for_the_occupied_number_in_the_second_chunk() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_next_available_sequence_number() => _result.AsT1.NextAvailableSequenceNumber.ShouldEqual(new EventSequenceNumber(1002));
    [Fact] void should_not_persist_any_of_the_batch() => _storedCount.ShouldEqual(1);
    [Fact] void should_check_occupancy_once_per_chunk() => _commands.Commands.Count(command => command.Contains("EXISTS", StringComparison.OrdinalIgnoreCase)).ShouldEqual(2);
}
