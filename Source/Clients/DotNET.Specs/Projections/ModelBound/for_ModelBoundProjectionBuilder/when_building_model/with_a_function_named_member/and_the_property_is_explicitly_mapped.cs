// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_a_function_named_member.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_a_function_named_member;

public class and_the_property_is_explicitly_mapped : Specification
{
    ModelBoundProjectionBuilder _builder;
    ProjectionDefinition _result;

    void Establish() => _builder = new(new CamelCaseNamingPolicy(), new EventTypesForSpecifications([typeof(WeekRecorded)]));
    void Because() => _result = _builder.Build(typeof(WeekModel));

    [Fact] void should_name_the_target_and_source_members() => _result.From.Values.Single().Properties["week"].ShouldEqual("week");
    [Fact] void should_name_the_auto_map_exclusion() => _result.NoAutoMapProperties.ShouldContain("week");
}
