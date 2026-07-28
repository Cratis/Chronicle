// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

/// <summary>
/// Guards the routing key round-trip: the re-homing looks the child up by the naming-policy-converted property
/// name, so it must match the same-policy key the child definition was stored under. A camel-casing policy
/// exercises that round-trip, which the identity test policy cannot.
/// </summary>
public class with_children_having_property_level_removed_with_under_camel_casing : Specification
{
    ModelBoundProjectionBuilder _builder;
    INamingPolicy _camelCasingPolicy;
    ProjectionDefinition _result;
    ChildrenDefinition _itemsDef;

    void Establish()
    {
        _camelCasingPolicy = new given.CamelCasingNamingPolicy();
        var eventTypes = new EventTypesForSpecifications([typeof(ItemAddedToCart), typeof(ChildItemRemoved)]);
        _builder = new ModelBoundProjectionBuilder(_camelCasingPolicy, eventTypes);
    }

    void Because()
    {
        _result = _builder.Build(typeof(ParentWithPropertyLevelRemovableChildren));
        _result.Children.TryGetValue("items", out _itemsDef);
    }

    [Fact] void should_key_the_child_under_the_camel_cased_name() => _result.Children.ContainsKey("items").ShouldBeTrue();
    [Fact] void should_not_have_removed_with_on_the_root() => _result.RemovedWith.Count.ShouldEqual(0);
    [Fact] void should_route_removed_with_to_the_child_despite_casing() => _itemsDef.RemovedWith.Count.ShouldEqual(1);
}
