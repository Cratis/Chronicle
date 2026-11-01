// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_getting_a_page_with_an_invalid_count : given.a_paged_event_sequence
{
    readonly Dictionary<int, Exception?> _exceptions = [];

    async Task Because()
    {
        foreach (var count in (int[])[-1, 0, 1001, int.MaxValue])
        {
            _exceptions[count] = await Catch.Exception(async () => await _eventSequence.GetPage(EventSequencePageRequest.Forward(null, count)));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1001)]
    [InlineData(int.MaxValue)]
    void should_reject_the_count_instead_of_clamping_it(int count) => _exceptions[count].ShouldBeOfExactType<ArgumentOutOfRangeException>();

    [Fact] void should_not_send_a_query() => _sequences.DidNotReceive().Page(Arg.Any<PageRequest>(), Arg.Any<CallContext>());
}
