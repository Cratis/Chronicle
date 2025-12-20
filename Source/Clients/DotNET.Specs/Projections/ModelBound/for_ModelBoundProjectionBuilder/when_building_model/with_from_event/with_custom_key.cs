// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_from_event;

public class with_custom_key : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(UserRegisteredWithCustomId)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(UserProfile));

    [Fact] void should_have_from_definition_for_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserRegisteredWithCustomId)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact] void should_map_email_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserRegisteredWithCustomId)).ToContract();
        var properties = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties;
        properties.Keys.ShouldContain(nameof(UserProfile.Email));
    }

    [Fact] void should_map_name_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserRegisteredWithCustomId)).ToContract();
        var properties = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties;
        properties.Keys.ShouldContain(nameof(UserProfile.Name));
    }

    [Fact] void should_use_custom_key_from_event()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserRegisteredWithCustomId)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Key.ShouldEqual(nameof(UserRegisteredWithCustomId.UserId));
    }

    [Fact] void should_apply_naming_policy_to_custom_key()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserRegisteredWithCustomId)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Key.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(UserRegisteredWithCustomId.UserId))));
    }
}
