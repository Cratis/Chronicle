// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_looking_up_an_ambiguous_key_when_no_legacy_rows_exist : given.a_sink_with_composite_keys
{
    bool _foundBefore;
    bool _foundAfter;

    async Task Because()
    {
        _foundBefore = await NameOf(KeyOf("a_b", "c")) is not null;
        await Write(KeyOf("a_b", "c"), "new");
        _foundAfter = await NameOf(KeyOf("a_b", "c")) is not null;
    }

    [Fact] void should_not_find_anything_before_writing() => _foundBefore.ShouldBeFalse();
    [Fact] void should_find_the_written_row() => _foundAfter.ShouldBeTrue();
}
