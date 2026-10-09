// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

using context = Cratis.Chronicle.Kernel.Integration.EventRouting.when_round_tripping_batch_routing.and_the_stream_id_contains_delimiters_and_non_ascii_text.context;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.Kernel.Integration.EventRouting.when_round_tripping_batch_routing;

[Collection(ChronicleCollection.Name)]
public class and_the_stream_id_contains_delimiters_and_non_ascii_text(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : when_round_tripping_routing.given.a_storage_for_routing(fixture)
    {
        async Task Because()
        {
            AppendSucceeded = (await AppendManyWithRouting("Ærø|50%")).IsSuccess;
            ReadBack = await _storage.GetEventAt(EventSequenceNumber.First);
        }
    }

    [Fact] void should_succeed() => Context.AppendSucceeded.ShouldBeTrue();
    [Fact] void should_read_back_the_event_source_type() => Context.ReadBack.Context.EventSourceType.Value.ShouldEqual("Account");
    [Fact] void should_read_back_the_event_stream_type() => Context.ReadBack.Context.EventStreamType.Value.ShouldEqual("Payments");
    [Fact] void should_read_back_the_exact_stream_id() => Context.ReadBack.Context.EventStreamId.Value.ShouldEqual("Ærø|50%");
}
