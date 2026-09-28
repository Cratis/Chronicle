// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

public class when_using_legacy_positional_append_shapes : given.a_legacy_event_sequence
{
    void Because()
    {
        var source = EventSourceId.New();
        EventForEventSourceId[] routed = [new(source, "event")];
        _sequence.Append(source, "event", null).GetAwaiter().GetResult();
        _sequence.Append(source, "event", default).GetAwaiter().GetResult();
        _sequence.AppendMany(source, ["event"], null).GetAwaiter().GetResult();
        _sequence.AppendMany(source, ["event"], default).GetAwaiter().GetResult();
        _sequence.AppendMany(routed, null).GetAwaiter().GetResult();
        _sequence.AppendMany(routed, default).GetAwaiter().GetResult();

        var transactional = Substitute.For<ITransactionalEventSequence>();
        transactional.Append(source, "event", null).GetAwaiter().GetResult();
        transactional.Append(source, "event", default).GetAwaiter().GetResult();
        transactional.AppendMany(source, ["event"], null).GetAwaiter().GetResult();
        transactional.AppendMany(source, ["event"], default).GetAwaiter().GetResult();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.AddEvent(EventSequenceId.Log, source, "event", null!);
        unitOfWork.AddEvent(EventSequenceId.Log, source, "event", default!);
    }

    [Fact] void should_keep_the_legacy_overloads_callable() => (_implementation.AppendCalls, _implementation.SingleSourceBatchCalls, _implementation.RoutedBatchCalls).ShouldEqual((2, 2, 2));
}
