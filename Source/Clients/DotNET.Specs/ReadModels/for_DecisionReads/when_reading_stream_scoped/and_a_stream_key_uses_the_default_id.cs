// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_stream_scoped;

public class and_a_stream_key_uses_the_default_id : given.a_decision_reader
{
    Exception _error;

    void Establish() => _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
    async Task Because() => _error = await Catch.Exception(() => _reader.GetDetached<Model>("source", "stream-type", EventStreamId.Default));

    [Fact] void should_require_an_explicit_stream_id() => ((DecisionReadRefused)_error).Reason.ShouldEqual(DecisionReadRefusalReason.StreamKeyRequiresExplicitStreamId);
    [Fact] void should_not_read_before_refusing() => _readModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_probe_before_refusing() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
