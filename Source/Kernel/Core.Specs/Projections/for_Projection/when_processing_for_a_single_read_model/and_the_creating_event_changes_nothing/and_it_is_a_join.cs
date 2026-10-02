// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model.and_the_creating_event_changes_nothing;

public class and_it_is_a_join : given.a_projection_grain_with_a_child_projection
{
    ExpandoObject _result;

    void Establish() => RootOperationType = ProjectionOperationType.Join;

    async Task Because() => _result = await ProcessTheEvent();

    [Fact] void should_not_return_a_read_model() => _result.ShouldBeEmpty();
}
