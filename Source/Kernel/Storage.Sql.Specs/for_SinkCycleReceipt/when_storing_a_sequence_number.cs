// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.for_SinkCycleReceipt;

public class when_storing_a_sequence_number : Specification
{
    static readonly ulong[] _values = [0, 1, 9223372036854775807UL, 9223372036854775808UL, ulong.MaxValue - 1, ulong.MaxValue];
    ulong[] _roundTripped;
    bool _threw;

    void Because()
    {
        try
        {
            _roundTripped = [.. _values.Select(value => SinkCycleReceipt.FromStored(SinkCycleReceipt.ToStored(value)))];
        }
        catch (OverflowException)
        {
            _threw = true;
        }
    }

    [Fact] void should_not_overflow() => _threw.ShouldBeFalse();
    [Fact] void should_keep_every_value_including_the_sentinels() => _roundTripped.ShouldEqual(_values);
    [Fact] void should_store_the_unavailable_sentinel_as_minus_one() => SinkCycleReceipt.ToStored(ulong.MaxValue).ShouldEqual(-1L);
}
