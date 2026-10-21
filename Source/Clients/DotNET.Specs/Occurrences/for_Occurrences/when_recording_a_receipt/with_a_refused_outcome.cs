// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_recording_a_receipt;

public class with_a_refused_outcome : given.namespace_bound_occurrences
{
    async Task Because() => _result = await _occurrences.RecordEffectReceipt(_key, new Refused(["business rule"]), JsonValue.Create("refused"));

    [Fact] void should_bind_the_event_store() => _request.EventStore.ShouldEqual("store");
    [Fact] void should_bind_the_namespace() => _request.Namespace.ShouldEqual("tenant");
    [Fact] void should_send_the_refused_outcome() => _request.Outcome.ShouldEqual(Contracts.Storage.Occurrences.EffectOutcome.Refused);
    [Fact] void should_keep_the_reasons() => ((Refused)_result.Receipt.Outcome).Reasons.ShouldContainOnly("business rule");
    [Fact] void should_round_trip_the_response() => _result.Receipt.Response.ToJsonString().ShouldEqual("\"refused\"");
}
