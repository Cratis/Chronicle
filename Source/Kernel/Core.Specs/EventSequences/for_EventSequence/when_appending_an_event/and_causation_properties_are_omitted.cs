// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_causation_properties_are_omitted : given.an_event_sequence_omitting_causation_properties
{
    AppendResult _result;

    async Task Because()
    {
        _result = await _eventSequence.Append(
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

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_omit_all_property_values() => _storedCausation.All(causation => causation.Properties.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_the_types() => _storedCausation.Select(causation => causation.Type).ShouldEqual(_requestedCausation.Select(causation => causation.Type));
    [Fact] void should_keep_the_times() => _storedCausation.Select(causation => causation.Occurred).ShouldEqual(_requestedCausation.Select(causation => causation.Occurred));
    [Fact] void should_leave_the_request_properties_unchanged() => _requestedCausation.Select(causation => causation.Properties.Values.Single()).ShouldEqual(["personal value", "another value"]);
    [Fact] void should_not_reuse_request_entries() => ReferenceEquals(_requestedCausation[0], _storedCausation[0]).ShouldBeFalse();
}
