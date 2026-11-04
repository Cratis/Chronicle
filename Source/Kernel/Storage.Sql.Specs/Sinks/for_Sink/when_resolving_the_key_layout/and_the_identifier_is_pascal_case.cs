// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_resolving_the_key_layout;

public class and_the_identifier_is_pascal_case : given.a_sink_over_a_schema_with_an_identifier
{
    protected override string Identifier => "Id";

    async Task Because()
    {
        await Write("one", "first");
        await Write("two", "second");
        await Replay();
    }

    [Fact] void should_not_read_the_layout() => _layoutReads.ShouldEqual(0);
}
