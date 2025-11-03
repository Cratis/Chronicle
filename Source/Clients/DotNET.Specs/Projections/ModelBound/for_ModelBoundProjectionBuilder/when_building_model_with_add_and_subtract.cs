// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder;

public class when_building_model_with_add_and_subtract : given.a_model_bound_projection_builder
{
    ProjectionDefinition? _result;

    void Because() => _result = builder.Build(typeof(AccountInfo));

    [Fact] void should_have_from_definition_for_deposit()
    {
        var eventType = new EventType
        {
            Id = "41b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f",
            Generation = 1
        };
        _result!.From.Keys.ShouldContain(eventType);
    }

    [Fact] void should_use_add_expression_for_balance()
    {
        var eventType = new EventType
        {
            Id = "41b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f",
            Generation = 1
        };
        var expression = _result!.From[eventType].Properties["Balance"];
        expression.ShouldContain("$add(");
    }

    [Fact] void should_have_from_definition_for_withdrawal()
    {
        var eventType = new EventType
        {
            Id = "51b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f",
            Generation = 1
        };
        _result!.From.Keys.ShouldContain(eventType);
    }

    [Fact] void should_use_subtract_expression_for_balance()
    {
        var eventType = new EventType
        {
            Id = "51b5c5c4-5b8a-4c4f-8c4f-1b8a5c4c5c4f",
            Generation = 1
        };
        var expression = _result!.From[eventType].Properties["Balance"];
        expression.ShouldContain("$subtract(");
    }
}
