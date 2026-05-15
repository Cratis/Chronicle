// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_updating_projection_definition.for_materialized_projection.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_updating_projection_definition;

[Collection(ChronicleCollection.Name)]
public class for_immediate_projection(context context) : Given<context>(context)
{
    [Fact] void should_have_first_result_with_name_only() => Context.Result.Name.ShouldEqual("First Name");
    [Fact] void should_not_have_description_in_first_result() => Context.Result.Description.ShouldBeNull();
    [Fact] void should_have_updated_result_with_name() => Context.ResultAfterUpdate.Name.ShouldEqual("Second Name");
    [Fact] void should_have_description_in_updated_result() => Context.ResultAfterUpdate.Description.ShouldEqual("Second Description");
}
