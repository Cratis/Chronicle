// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_a_bound_is_a_special_sequence_number : given.a_page_query
{
    readonly Dictionary<(ulong Bound, bool Backward, bool IsFrom), Exception?> _exceptions = [];

    async Task Because()
    {
        foreach (var bound in (ulong[])[ulong.MaxValue, ulong.MaxValue - 1, ulong.MaxValue - 2])
        {
            foreach (var backward in (bool[])[false, true])
            {
                _exceptions[(bound, backward, true)] = await Catch.Exception(async () => await Query(from: bound, backward: backward));
                _exceptions[(bound, backward, false)] = await Catch.Exception(async () => await Query(backward: backward, until: bound));
            }
        }
    }

    [Theory]
    [InlineData(ulong.MaxValue)]
    [InlineData(ulong.MaxValue - 1)]
    [InlineData(ulong.MaxValue - 2)]
    void should_reject_special_from_and_until_in_both_directions(ulong bound)
    {
        _exceptions[(bound, false, true)].ShouldBeOfExactType<InvalidEventSequencePage>();
        _exceptions[(bound, false, false)].ShouldBeOfExactType<InvalidEventSequencePage>();
        _exceptions[(bound, true, true)].ShouldBeOfExactType<InvalidEventSequencePage>();
        _exceptions[(bound, true, false)].ShouldBeOfExactType<InvalidEventSequencePage>();
    }

    [Fact] void should_not_access_storage() => _storage.DidNotReceiveWithAnyArgs().GetEventStore(default!);
}
