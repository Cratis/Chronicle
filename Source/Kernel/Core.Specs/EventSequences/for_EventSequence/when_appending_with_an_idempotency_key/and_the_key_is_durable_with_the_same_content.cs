// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_is_durable_with_the_same_content : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => Durable();
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_report_already_appended() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_return_the_stored_slot() => _result.Result.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_skip_concurrency() => _result.Result.ConcurrencyCheckPerformed.ShouldBeFalse();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
    [Fact] async Task should_not_enqueue() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] async Task should_not_apply_compliance() => await _complianceManager.DidNotReceive().Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] async Task should_not_report_compliance_keys() => await _complianceManager.DidNotReceive().ApplyReportingKeys(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] void should_not_update_constraints() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
