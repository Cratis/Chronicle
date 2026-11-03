// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence.when_getting_tail_sequence_number_with_cancellation;

public class from_a_legacy_implementation : given.a_legacy_event_sequence
{
    List<Exception> _errors;
    bool _enumerated;
    int _callsBeforeLegacyRead;
    EventSequenceNumber _legacyResult;

    async Task Because()
    {
        using var active = new CancellationTokenSource();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _errors = [];
        foreach (var token in new[] { CancellationToken.None, active.Token, cancelled.Token })
        {
            _errors.Add(await Catch.Exception(() => _sequence.GetTailSequenceNumberWithCancellation(token, filterEventTypes: Poison()).WaitAsync(TimeSpan.FromSeconds(10))));
        }
        _callsBeforeLegacyRead = _implementation.TailCalls;
        _legacyResult = await _sequence.GetTailSequenceNumber().WaitAsync(TimeSpan.FromSeconds(10));
    }

    IEnumerable<EventType> Poison() => Enumerable.Repeat(default(EventType)!, 1).Select<EventType, EventType>(_ =>
    {
        _enumerated = true;
        throw new InvalidOperationException("Legacy DIM must not enumerate filters");
    });

    [Fact] void should_refuse_none() => _errors[0].ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_refuse_a_cancellable_token() => _errors[1].ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_refuse_a_cancelled_token() => _errors[2].ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_not_enumerate_filters() => _enumerated.ShouldBeFalse();
    [Fact] void should_not_fall_back_to_the_old_method() => _callsBeforeLegacyRead.ShouldEqual(0);
    [Fact] void should_preserve_the_old_method() => _legacyResult.ShouldEqual((EventSequenceNumber)5460UL);
    [Fact] void should_call_the_old_method_only_when_requested() => _implementation.TailCalls.ShouldEqual(1);
}
