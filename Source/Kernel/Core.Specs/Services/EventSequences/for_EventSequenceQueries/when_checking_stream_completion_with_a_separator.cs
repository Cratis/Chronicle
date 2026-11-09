// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.EventSequences.for_EventSequenceQueries;

public class when_checking_stream_completion_with_a_separator : for_QueryExecutor.given.a_routed_query
{
    IGrainFactory _grainFactory;
    QueryResult<bool> _result;

    void Establish() => _grainFactory = Substitute.For<IGrainFactory>();

    async Task Because() => _result = await new EventSequenceQueries(_grainFactory, NamespaceCasingContext.Create(), NullLogger<EventSequenceQueries>.Instance)
        .IsStreamCompleted(
            new IsStreamCompletedRequest { EventStore = "bench", Namespace = "Default", EventSequenceId = "event-log#System#Default", EventStreamType = "t", EventStreamId = "s" },
            CallFor(new Probe(), CallerNamed(Administrator)));

    [Fact] void should_return_a_validation_failure() => _result.ValidationResults.Single().Message.ShouldEqual(SystemStoreAccess.SeparatorRefused);
    [Fact] void should_not_ask_the_grain_factory() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
}
