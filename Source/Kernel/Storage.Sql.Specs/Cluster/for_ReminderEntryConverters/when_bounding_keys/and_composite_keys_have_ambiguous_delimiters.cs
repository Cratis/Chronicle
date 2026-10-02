// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderEntryConverters.when_bounding_keys;

public class and_composite_keys_have_ambiguous_delimiters : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = ReminderEntryConverters.GetRowKey(GrainId.Create("observer", new string('x', 200)), "part-retry");
        _second = ReminderEntryConverters.GetRowKey(GrainId.Create("observer", new string('x', 200) + "-part"), "retry");
    }

    [Fact] void should_keep_different_pairs_distinct() => _first.ShouldNotEqual(_second);
    [Fact] void should_use_a_fixed_length_key() => _first.Length.ShouldEqual(71);
}
