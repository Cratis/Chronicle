// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_event_source_id;

public class on_root_members : given.a_model_bound_projection_builder
{
    FromDefinition _result;
    FromDefinition _fluent;

    void Because()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        _result = builder.Build(typeof(Account)).From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        var fluent = new ProjectionBuilderFor<Account>(
            new ProjectionId(typeof(Account).FullName), typeof(Account), naming_policy, event_types, new JsonSerializerOptions());
        fluent.From<DebitAccountOpened>(_ => _
            .Set(model => model.Source).ToEventSourceId()
            .Set(model => model.PropertySource).ToEventSourceId()
            .SetOnce(model => model.OriginalSource).ToEventSourceId()
            .Set(model => model.LegacySource).ToEventContextProperty(context => context.EventSourceId));
        _fluent = fluent.Build().From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
    }

    [Fact] void should_map_a_constructor_parameter_to_the_event_source_id() => _result.Properties[nameof(Account.Source)].ShouldEqual("$eventSourceId");
    [Fact] void should_map_a_property_to_the_event_source_id() => _result.Properties[nameof(Account.PropertySource)].ShouldEqual("$eventSourceId");
    [Fact] void should_support_setting_the_event_source_id_once() => _result.Properties[nameof(Account.OriginalSource)].ShouldEqual("$setOnce($eventSourceId)");
    [Fact] void should_preserve_the_existing_context_mapping() => _result.Properties[nameof(Account.LegacySource)].ShouldEqual("$eventContext(EventSourceId)");
    [Fact] void should_produce_the_same_property_mappings_as_the_fluent_mapping() => _result.Properties.ShouldContainOnly(_fluent.Properties.ToArray());
    [Fact] void should_produce_the_same_key_as_the_fluent_mapping() => _result.Key.ShouldEqual(_fluent.Key);

    record Account(
        [Key] Guid Id,
        [SetFromEventSourceId<DebitAccountOpened>] EventSourceId Source,
        [property: SetFromEventSourceId<DebitAccountOpened>] EventSourceId PropertySource,
        [SetFromEventSourceId<DebitAccountOpened>(SetOnce = true)] EventSourceId OriginalSource,
        [SetFromContext<DebitAccountOpened>(nameof(EventContext.EventSourceId))] EventSourceId LegacySource);
}
