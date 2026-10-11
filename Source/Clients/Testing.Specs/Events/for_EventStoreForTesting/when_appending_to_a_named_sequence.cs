// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_appending_to_a_named_sequence : Specification
{
    EventStoreForTesting _store;
    EventSequenceNumber _logTail;
    EventSequenceNumber _namedTail;
    int _logCount;
    int _namedCount;

    void Establish() => _store = new();

    async Task Because()
    {
        await _store.EventLog.Append(EventSourceId.New(), new MigrationTopicCreated("log"));
        var named = _store.GetEventSequence("named");
        await named.Append(EventSourceId.New(), new MigrationTopicCreated("named"));
        _logTail = await _store.GetEventSequence(EventSequenceId.Log).GetTailSequenceNumber();
        _namedTail = await _store.GetEventSequence("named").GetTailSequenceNumber();
        _logCount = (await _store.GetEventSequence(EventSequenceId.Log).GetFromSequenceNumber(EventSequenceNumber.First)).Count;
        _namedCount = (await named.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_share_the_log_with_the_sequence_lookup() => _logCount.ShouldEqual(1);
    [Fact] void should_keep_named_sequence_rows_separate() => _namedCount.ShouldEqual(1);
    [Fact] void should_start_the_log_at_zero() => _logTail.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_start_the_named_sequence_at_zero() => _namedTail.ShouldEqual(EventSequenceNumber.First);
}
