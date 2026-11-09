// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.EventSequences.for_EventSequenceQueries;

public class when_the_namespace_is_a_case_variant : for_QueryExecutor.given.a_routed_query
{
    IGrainFactory _grains;
    QueryResult<bool> _result;

    async Task Because()
    {
        _grains = Substitute.For<IGrainFactory>();
        var sequence = Substitute.For<IEventSequence>();
        _grains.GetGrain<IEventSequence>("event-log#store#Cratis").Returns(sequence);
        sequence.IsStreamCompleted(Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>()).Returns(true);
        var casing = NamespaceCasingContext.Create();
        casing.Canonicalize("store", "cratis").Returns((EventStoreNamespaceName)"Cratis");
        var query = new EventSequenceQueries(_grains, casing, NullLogger<EventSequenceQueries>.Instance);
        _result = await query.IsStreamCompleted(new IsStreamCompletedRequest
        {
            EventStore = "store",
            Namespace = "cratis",
            EventSequenceId = "event-log",
            EventStreamType = "type",
            EventStreamId = "stream"
        });
    }

    [Fact] void should_return_the_stored_namespaces_result() => _result.Data.ShouldBeTrue();
    [Fact] void should_not_fail() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_address_the_canonical_grain_key() => _grains.Received(1).GetGrain<IEventSequence>("event-log#store#Cratis");
    [Fact] void should_not_address_a_case_variant_grain_key() => _grains.DidNotReceive().GetGrain<IEventSequence>("event-log#store#cratis");
}
