// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Services.EventSequences;
using Cratis.Chronicle.Services.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_a_generated_query_addresses_a_namespace_after_its_sequence_id : Specification
{
    [Theory]
    [InlineData("event-log", true)]
    [InlineData("requested", false)]
    public async Task should_authorize_the_namespace_that_the_query_reads(string requestedNamespace, bool allowed)
    {
        using var caller = new an_entitled_caller(ns: "event-log");
        var grains = Substitute.For<IGrainFactory>();
        var sequence = Substitute.For<IEventSequence>();
        grains.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(sequence);
        sequence.IsStreamCompleted(Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>()).Returns(true);
        var query = new EventSequenceQueries(grains, NullLogger<EventSequenceQueries>.Instance);
        var request = new Contracts.EventSequences.IsStreamCompletedRequest
        {
            EventStore = "store",
            EventSequenceId = "event-log",
            Namespace = requestedNamespace,
            EventStreamType = "type",
            EventStreamId = "stream"
        };
        var result = await query.IsStreamCompleted(request, caller.Context);

        result.IsAuthorized.ShouldEqual(allowed);
        result.HasExceptions.ShouldBeFalse();
        result.Data.ShouldEqual(allowed);
        if (allowed) await sequence.Received(1).IsStreamCompleted(Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>());
        else await sequence.DidNotReceive().IsStreamCompleted(Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>());
    }
}
