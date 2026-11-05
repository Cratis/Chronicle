// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStreamKeyLength;

public class when_checking_whether_a_scope_fits : Specification
{
    static readonly EventSequenceId _sequence = new("seq");

    bool _atTheLimit;
    bool _overTheLimit;
    bool _oneColumnTooLong;
    bool _unsetPartsCountAsEmpty;

    void Because()
    {
        var remaining = ClosedStreamKeyLength.MaxTotalLength - _sequence.Value.Length;
        _atTheLimit = ClosedStreamKeyLength.Fits(_sequence, new(EventSourceId: new string('a', 200), EventStreamId: new string('b', remaining - 200)), string.Empty);
        _overTheLimit = ClosedStreamKeyLength.Fits(_sequence, new(EventSourceId: new string('a', 200), EventStreamId: new string('b', remaining - 199)), string.Empty);
        _oneColumnTooLong = ClosedStreamKeyLength.Fits(_sequence, new(EventSourceId: new string('a', 256)), string.Empty);
        _unsetPartsCountAsEmpty = ClosedStreamKeyLength.Fits(_sequence, new(), string.Empty);
    }

    [Fact] void should_accept_a_scope_at_the_total_limit() => _atTheLimit.ShouldBeTrue();
    [Fact] void should_refuse_a_scope_over_the_total_limit() => _overTheLimit.ShouldBeFalse();
    [Fact] void should_refuse_a_single_identifier_over_the_column_limit() => _oneColumnTooLong.ShouldBeFalse();
    [Fact] void should_count_parts_that_are_not_set_as_empty() => _unsetPartsCountAsEmpty.ShouldBeTrue();
}
