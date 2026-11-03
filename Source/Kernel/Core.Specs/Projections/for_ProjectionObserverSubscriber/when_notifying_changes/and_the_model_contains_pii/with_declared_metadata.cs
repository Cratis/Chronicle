// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_notifying_changes.and_the_model_contains_pii;

public class with_declared_metadata : given.a_change_for_a_model_with_pii
{
    protected override void DeclareMore(JsonSchema schema) =>
        schema.Properties[WellKnownProperties.LastHandledEventSequenceNumber] = new JsonSchemaProperty(
            WellKnownProperties.LastHandledEventSequenceNumber,
            new System.Text.Json.Nodes.JsonObject { ["type"] = "integer" },
            schema);

    async Task Because() => await _subscriber.OnNext("the-partition", [_event], new(null));

    [Fact] void should_carry_the_last_handled_event_sequence_number_on_the_document() => _document[WellKnownProperties.LastHandledEventSequenceNumber].GetValue<ulong>().ShouldEqual(42UL);
}
