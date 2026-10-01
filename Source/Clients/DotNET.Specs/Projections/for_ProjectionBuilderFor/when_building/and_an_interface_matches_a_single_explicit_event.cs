// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_an_interface_matches_a_single_explicit_event : events_with_derivatives
{
    ProjectionBuilderFor<Item> _builder;
    ProjectionDefinition _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(FirstItemChanged)]);
        _builder = new("items", typeof(Item), new DefaultNamingPolicy(), _eventTypes, new JsonSerializerOptions());
    }

    void Because()
    {
        _builder.From<FirstItemChanged>(from => from.UsingKey(_ => _.Description).Set(_ => _.Name).To(_ => _.Description));
        _builder.From<IItemChanged>(from => from.UsingParentKey(_ => _.Name).Set(_ => _.Id).To(_ => _.Name).Set(_ => _.Name).To(_ => _.Name));
        _result = _builder.Build();
    }

    [Fact] void should_register_only_the_concrete_event() => _result.From.Keys.Select(_ => _.ToClient()).ShouldContainOnly(typeof(FirstItemChanged).GetEventType());
    [Fact] void should_preserve_the_inherited_mapping() => _result.From.Single().Value.Properties[nameof(Item.Id)].ShouldEqual(nameof(IItemChanged.Name));
    [Fact] void should_prefer_the_explicit_mapping_for_conflicting_properties() => _result.From.Single().Value.Properties[nameof(Item.Name)].ShouldEqual(nameof(FirstItemChanged.Description));
    [Fact] void should_preserve_the_explicit_key() => _result.From.Single().Value.Key.ShouldEqual(nameof(FirstItemChanged.Description));
    [Fact] void should_not_replace_the_default_parent_key() => _result.From.Single().Value.ParentKey.ShouldEqual(string.Empty);
}
