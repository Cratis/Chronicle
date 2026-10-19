// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_from_every_sets_constants : Specification
{
    ProjectionDefinition _result;

    void Because()
    {
        var builder = new ProjectionBuilderFor<ConstantsModel>(
            new ProjectionId(typeof(ConstantsModel).FullName),
            typeof(ConstantsModel),
            new DefaultNamingPolicy(),
            new EventTypesForSpecifications([]),
            new JsonSerializerOptions());
        builder.FromEvery(_ => _
            .Set(model => model.Name).ToValue("constant")
            .Set(model => model.Concept).ToValue(new ConstantConcept("concept"))
            .Set(model => model.Status).ToValue(ConstantStatus.Active)
            .Set(model => model.LongStatus).ToValue(LongConstantStatus.Active)
            .Set(model => model.Optional).ToValue<string?>(null));
        _result = builder.Build();
    }

    [Fact] void should_not_subscribe_to_all_events() => _result.SubscribesToAllEvents.ShouldBeFalse();
    [Fact] void should_map_the_string_literal() => _result.All.Properties[nameof(ConstantsModel.Name)].ShouldEqual("$value(constant)");
    [Fact] void should_unwrap_the_concept() => _result.All.Properties[nameof(ConstantsModel.Concept)].ShouldEqual("$value(concept)");
    [Fact] void should_map_the_integer_enum_value() => _result.All.Properties[nameof(ConstantsModel.Status)].ShouldEqual("$value(1)");
    [Fact] void should_map_the_non_integer_enum_name() => _result.All.Properties[nameof(ConstantsModel.LongStatus)].ShouldEqual("$value(Active)");
    [Fact] void should_map_null() => _result.All.Properties[nameof(ConstantsModel.Optional)].ShouldEqual("$null");

    enum ConstantStatus { Active = 1 }
    enum LongConstantStatus : long { Active = 1 }
    record ConstantConcept(string Value) : ConceptAs<string>(Value);
    record ConstantsModel(string Name, ConstantConcept Concept, ConstantStatus Status, LongConstantStatus LongStatus, string? Optional);
}
