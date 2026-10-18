// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_ConcurrencyScopeConverters.when_converting_duplicate_event_source_scopes;

public class and_streams_differ : Specification
{
    bool _accepted;

    void Because() => _accepted = new EventSourceConcurrencyScope[]
    {
        new("source", new(5, true, "stream-type", "first")),
        new("source", new(5, true, "stream-type", "second"))
    }.ToChronicle().IsSuccess;

    [Fact] void should_preserve_both_stream_guards() => _accepted.ShouldBeTrue();
}
