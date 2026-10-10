// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_stream_scoped;

public class and_all_stream_types_are_requested : given.a_decision_reader
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_before_any_rpc_for_source_or_stream_keys(bool streamKeyed)
    {
        if (streamKeyed) _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
        var error = await Catch.Exception(() => _reader.GetDetached<Model>("source", EventStreamType.All, "stream-id"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.AllStreamsNotSupported);
        _readModels.ReceivedCalls().ShouldBeEmpty();
        _sequences.ReceivedCalls().ShouldBeEmpty();
        _projectionService.ReceivedCalls().ShouldBeEmpty();
    }
}
