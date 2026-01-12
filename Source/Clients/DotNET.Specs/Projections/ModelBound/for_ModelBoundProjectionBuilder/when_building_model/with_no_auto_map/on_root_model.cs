// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_no_auto_map;

public class on_root_model : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(UserRegistered)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(UserWithNoAutoMap));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_not_auto_map_properties()
    {
        var eventType = event_types.GetEventTypeFor(typeof(UserRegistered)).ToContract();
        var fromDef = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // With NoAutoMap attribute, properties should not be automatically mapped
        fromDef.Properties.Count.ShouldEqual(0);
    }
}

[EventType]
public record UserRegistered(string Name, string Email);

[NoAutoMap]
[FromEvent<UserRegistered>]
public record UserWithNoAutoMap([Key] Guid Id, string Name, string Email);
