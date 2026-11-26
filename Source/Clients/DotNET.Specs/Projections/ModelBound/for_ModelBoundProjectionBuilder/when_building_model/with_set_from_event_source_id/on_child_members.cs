// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_event_source_id;

public class on_child_members : given.a_model_bound_projection_builder
{
    FromDefinition _created;
    FromDefinition _updated;

    void Establish() => builder = new ModelBoundProjectionBuilder(new given.CamelCasingNamingPolicy(), event_types);

    void Because()
    {
        var child = builder.Build(typeof(Accounts)).Children["entries"];
        _created = child.From.Single(kvp => kvp.Key.IsEqual(event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract())).Value;
        _updated = child.From.Single(kvp => kvp.Key.IsEqual(event_types.GetEventTypeFor(typeof(DepositToDebitAccountPerformed)).ToContract())).Value;
    }

    [Fact] void should_keep_the_explicit_parameter_mapping_instead_of_the_automatic_identifier_mapping() => _created.Properties["id"].ShouldEqual("$eventSourceId");
    [Fact] void should_map_the_property_for_the_declared_event() => _updated.Properties["lastSource"].ShouldEqual("$eventSourceId");
    [Fact] void should_map_the_parameter_for_the_declared_event() => _updated.Properties["parameterSource"].ShouldEqual("$eventSourceId");
    [Fact] void should_not_map_the_other_event_property_for_the_creating_event() => _created.Properties.Keys.ShouldNotContain("lastSource");
    [Fact] void should_not_map_the_other_event_parameter_for_the_creating_event() => _created.Properties.Keys.ShouldNotContain("parameterSource");
    [Fact] void should_not_map_the_creating_event_identifier_for_the_other_event() => _updated.Properties.Keys.ShouldNotContain("id");
    [Fact] void should_support_setting_a_child_property_once() => _created.Properties["originalSource"].ShouldEqual("$setOnce($eventSourceId)");
    [Fact] void should_preserve_the_existing_child_context_mapping() => _created.Properties["legacySource"].ShouldEqual("$eventContext(eventSourceId)");

    record Accounts(
        [Key] Guid Id,
        [ChildrenFrom<DebitAccountOpened>] IEnumerable<Entry> Entries);

    record Entry(
        [SetFromEventSourceId<DebitAccountOpened>] EventSourceId Id,
        [property: SetFromEventSourceId<DepositToDebitAccountPerformed>] EventSourceId LastSource,
        [SetFromEventSourceId<DepositToDebitAccountPerformed>] EventSourceId ParameterSource,
        [property: SetFromEventSourceId<DebitAccountOpened>(SetOnce = true)] EventSourceId OriginalSource,
        [SetFromContext<DebitAccountOpened>(nameof(EventContext.EventSourceId))] EventSourceId LegacySource);
}
