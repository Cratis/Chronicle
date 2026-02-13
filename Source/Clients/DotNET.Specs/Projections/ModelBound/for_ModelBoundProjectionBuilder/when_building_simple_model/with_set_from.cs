// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_simple_model;

public class with_set_from : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(AccountInfo));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_projection_id_from_type_name() => _result.Identifier.ShouldEqual(typeof(AccountInfo).FullName);
    [Fact] void should_be_active() => _result.IsActive.ShouldBeTrue();
    [Fact] void should_be_rewindable() => _result.IsRewindable.ShouldBeTrue();
    [Fact] void should_have_three_from_definitions() => _result.From.Count.ShouldEqual(3);

    [Fact]
    void should_have_from_definition_for_debit_account_opened()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        _result.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_map_name_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(DebitAccountOpened)).ToContract();
        _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value.Properties.Keys.ShouldContain(nameof(AccountInfo.Name));
    }
}
