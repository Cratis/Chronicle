// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model;

public class with_multiple_properties_from_same_event : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(ProductRegistered)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(ProductInfo));

    [Fact] void should_have_single_from_definition() => _result.From.Count.ShouldEqual(1);

    [Fact] void should_have_from_definition_for_product_registered()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductRegistered)).ToContract();
        _result.From.Keys.Count(et => et.Id == eventType.Id && et.Generation == eventType.Generation).ShouldEqual(1);
    }

    [Fact] void should_have_three_properties_in_definition()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductRegistered)).ToContract();
        var entry = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation);
        entry.Value.Properties.Count.ShouldEqual(3);
    }

    [Fact] void should_map_name_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductRegistered)).ToContract();
        var entry = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation);
        entry.Value.Properties.Keys.ShouldContain(nameof(ProductInfo.Name));
    }

    [Fact] void should_map_description_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductRegistered)).ToContract();
        var entry = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation);
        entry.Value.Properties.Keys.ShouldContain(nameof(ProductInfo.Description));
    }

    [Fact] void should_map_price_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductRegistered)).ToContract();
        var entry = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation);
        entry.Value.Properties.Keys.ShouldContain(nameof(ProductInfo.Price));
    }
}
