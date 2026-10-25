// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

public class when_a_legacy_implementation_does_not_support_paging : given.a_legacy_event_sequence
{
    Exception? _pageException;
    Exception? _latestException;

    async Task Because()
    {
        _pageException = await Catch.Exception(async () => await _sequence.GetPage(EventSequencePageRequest.Forward(null, 1)));
        _latestException = await Catch.Exception(async () => await _sequence.GetLatest(1));
    }

    [Fact] void should_fail_clearly_for_paged_reads() => _pageException.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_fail_clearly_for_latest_reads() => _latestException.ShouldBeOfExactType<NotSupportedException>();
}
