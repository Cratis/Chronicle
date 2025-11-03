// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder;

public class when_building_simple_model_with_set_from : given.a_model_bound_projection_builder
{
    ProjectionDefinition? _result;

    void Because() => _result = builder.Build(typeof(AccountInfo));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_projection_id_from_type_name() => _result!.Identifier.ShouldEqual(typeof(AccountInfo).FullName);
    [Fact] void should_be_active() => _result!.IsActive.ShouldBeTrue();
    [Fact] void should_be_rewindable() => _result!.IsRewindable.ShouldBeTrue();
    [Fact] void should_have_three_from_definitions() => _result!.From.Count.ShouldEqual(3);

    [Fact] void should_have_from_definition_for_debit_account_opened()
    {
        var eventType = new EventType
        {
            Id = "31b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f",
            Generation = 1
        };
        _result!.From.Keys.ShouldContain(eventType);
    }

    [Fact] void should_map_name_property()
    {
        var eventType = new EventType
        {
            Id = "31b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f",
            Generation = 1
        };
        _result!.From[eventType].Properties.Keys.ShouldContain("Name");
    }
}
