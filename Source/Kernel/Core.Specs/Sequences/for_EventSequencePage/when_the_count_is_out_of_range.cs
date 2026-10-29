// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_the_count_is_out_of_range : given.a_page_query
{
    readonly Dictionary<int, Exception?> _exceptions = [];

    async Task Because()
    {
        foreach (var count in (int[])[int.MinValue, -1, 0, 1001, int.MaxValue])
        {
            _exceptions[count] = await Catch.Exception(async () => await Query(count: count));
        }
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1001)]
    [InlineData(int.MaxValue)]
    void should_reject_invalid_counts(int count) => _exceptions[count].ShouldBeOfExactType<InvalidEventSequencePage>();

    [Fact] void should_not_access_storage() => _storage.DidNotReceiveWithAnyArgs().GetEventStore(default!);
}
