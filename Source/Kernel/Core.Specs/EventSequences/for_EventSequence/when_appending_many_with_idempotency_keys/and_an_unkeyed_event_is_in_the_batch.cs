// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_an_unkeyed_event_is_in_the_batch : given.an_idempotent_batch
{
    void Establish()
    {
        DurableAt(0, 4);
        _batchKeys[1] = null;
        _batchKeys[2] = null;
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_succeed() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_leave_unkeyed_identities_empty() => _written.TrueForAll(@event => @event.Identity is null).ShouldBeTrue();
    [Fact] void should_append_both_unkeyed_events() => _written.Count.ShouldEqual(2);
    [Fact] async Task should_use_ordinary_compliance_for_unkeyed_events() => await _complianceManager.Received(2).Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] async Task should_not_report_keys_for_unkeyed_events() => await _complianceManager.DidNotReceive().ApplyReportingKeys(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
}
