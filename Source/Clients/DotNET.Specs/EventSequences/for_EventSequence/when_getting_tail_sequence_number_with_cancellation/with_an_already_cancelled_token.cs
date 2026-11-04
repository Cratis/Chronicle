// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation;

public class with_an_already_cancelled_token : given.an_asynchronous_event_sequence
{
    Exception _error;
    bool _enumerated;

    async Task Because()
    {
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(() => _eventSequence.GetTailSequenceNumberWithCancellation(_cancellation.Token, filterEventTypes: Poison()).WaitAsync(TimeSpan.FromSeconds(10)));
    }

    IEnumerable<EventType> Poison() => Enumerable.Repeat(default(EventType)!, 1).Select<EventType, EventType>(_ =>
    {
        _enumerated = true;
        throw new InvalidOperationException("Filters must not be enumerated");
    });

    [Fact] void should_cancel() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_preserve_the_token() => ((OperationCanceledException)_error).CancellationToken.ShouldEqual(_cancellation.Token);
    [Fact] void should_not_enumerate_filters() => _enumerated.ShouldBeFalse();
    [Fact] void should_not_acquire_services() => _asynchronous.AcquisitionCalls.ShouldEqual(0);
    [Fact] void should_not_acquire_synchronously() => _ = _accessor.DidNotReceive().Services;
    [Fact] void should_not_call_the_rpc() => _rpcCalls.ShouldEqual(0);
}
