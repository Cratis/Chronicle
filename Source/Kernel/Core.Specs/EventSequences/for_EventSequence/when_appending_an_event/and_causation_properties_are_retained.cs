// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_causation_properties_are_retained : given.an_event_sequence_omitting_causation_properties
{
    protected override CausationPropertyRetention CausationPropertyRetention => CausationPropertyRetention.Retain;

    async Task Because()
    {
        await _eventSequence.Append(
            EventSourceType.Default,
            _eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            new JsonObject(),
            CorrelationId.New(),
            _requestedCausation,
            Identity.System,
            [],
            ConcurrencyScope.None);
        _storedCausation = StoredAppend();
    }

    [Fact] void should_pass_the_original_causation_chain_to_storage() => ReferenceEquals(_requestedCausation, _eventSequenceStorage.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Append").GetArguments()[7]).ShouldBeTrue();
    [Fact] void should_keep_the_original_properties() => _storedCausation.Select(causation => causation.Properties.Values.Single()).ShouldEqual(["personal value", "another value"]);
}
