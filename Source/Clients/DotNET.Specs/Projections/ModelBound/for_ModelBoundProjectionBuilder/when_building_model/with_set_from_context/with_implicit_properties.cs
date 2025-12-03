// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_context;

public class with_implicit_properties : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(EventAuditEntry));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_projection_id_from_type_name() => _result.Identifier.ShouldEqual(typeof(EventAuditEntry).FullName);
    [Fact] void should_have_from_definition_for_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_map_occurred_property_to_event_context()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(EventAuditEntry.Occurred)].ShouldEqual("$eventContext(Occurred)");
    }

    [Fact] void should_map_sequence_number_property_to_event_context()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(EventAuditEntry.SequenceNumber)].ShouldEqual("$eventContext(SequenceNumber)");
    }

    record EventAuditEntry(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        string EventName,

        [SetFromContext<DebitAccountOpened>]
        DateTimeOffset Occurred,

        [SetFromContext<DebitAccountOpened>]
        ulong SequenceNumber);
}
