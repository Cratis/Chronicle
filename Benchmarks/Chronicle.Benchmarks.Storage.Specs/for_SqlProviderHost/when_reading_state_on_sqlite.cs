// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SqlProviderHost;

public class when_reading_state_on_sqlite : Specification
{
    SqlProviderHost _host = default!;
    EventSequenceState _state = default!;
    EventSequenceState _otherState = default!;
    CommandSnapshot _snapshot = default!;

    async Task Establish()
    {
        _host = new(StorageProvider.Sqlite);
        await _host.Start();
    }

    async Task Because()
    {
        var storage = _host.CreateSequence();
        await storage.SaveState(new EventSequenceState
        {
            SequenceNumber = 43,
            TailSequenceNumberPerEventType = new Dictionary<Concepts.Events.EventTypeId, Concepts.Events.EventSequenceNumber>
            {
                [BenchmarkEvents.Selected.Id] = 42
            }
        });
        var otherStorage = _host.CreateSequence();
        await otherStorage.SaveState(new EventSequenceState { SequenceNumber = 7 });
        _host.Counter.Reset();
        _state = await storage.GetState();
        _otherState = await otherStorage.GetState();
        _snapshot = _host.Counter.Snapshot();
    }

    async Task Destroy() => await _host.DisposeAsync();

    [Fact] void should_read_the_saved_next_sequence_number() => _state.SequenceNumber.Value.ShouldEqual(43UL);
    [Fact] void should_read_the_saved_tail() => _state.TailSequenceNumberPerEventType[BenchmarkEvents.Selected.Id].Value.ShouldEqual(42UL);
    [Fact] void should_keep_sequence_states_separate() => _otherState.SequenceNumber.Value.ShouldEqual(7UL);
    [Fact] void should_count_the_state_reads() => _snapshot.Total.ShouldEqual(2L);
}
