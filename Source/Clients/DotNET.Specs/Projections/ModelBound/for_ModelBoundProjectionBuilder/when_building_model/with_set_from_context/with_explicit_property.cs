// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_context;

public class with_explicit_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(EventAuditEntry));

    [Fact] void should_map_timestamp_property_to_occurred_on_event_context()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(EventAuditEntry.Timestamp)].ShouldEqual("$eventContext(Occurred)");
    }

    record EventAuditEntry(
        [Key]
        Guid Id,

        [SetFromContext<DebitAccountOpened>("Occurred")]
        DateTimeOffset Timestamp);
}
