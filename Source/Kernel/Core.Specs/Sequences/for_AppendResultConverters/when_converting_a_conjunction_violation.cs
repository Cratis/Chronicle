// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Sequences.for_AppendResultConverters;

public class when_converting_a_conjunction_violation : Specification
{
    Contracts.EventSequences.Concurrency.ConcurrencyViolation _result;

    void Because() => _result = new ConcurrencyViolation("source", 5UL, 6UL)
    {
        ScopeIndex = 2,
        Scope = new(5, true, "stream", "second")
    }.ToContract();

    [Fact] void should_keep_the_position() => _result.ScopeIndex.ShouldEqual((uint?)2);
    [Fact] void should_keep_the_failed_stream() => _result.Scope!.EventStreamId.ShouldEqual("second");
    [Fact] void should_keep_the_expectation() => _result.Scope!.SequenceNumber.ShouldEqual(5UL);
}
