// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Engine.given;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model.and_the_event_reaches_a_child_only_through_a_registration;

public class and_it_is_a_root_derivative_a_child_also_handles : given.a_projection_grain_with_a_child_projection
{
    ExpandoObject _result;

    async Task Establish() => ReportOperationTypesOf(await ProjectionWithChildOnlyEvents.Create(), ProjectionWithChildOnlyEvents.RootDerived);

    async Task Because() => _result = await ProcessTheEvent();

    [Fact] void should_return_the_read_model_identified_by_its_key() => ((IDictionary<string, object?>)_result)["id"].ShouldEqual(_rootKey.Value.ToString());
}
