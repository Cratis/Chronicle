// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder;

public class when_building_a_unique_constraint : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;
    bool _builderCallbackCalled;

    void Because()
    {
        _constraintBuilder.Unique(_ => _builderCallbackCalled = true);
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_call_the_builder_callback() => _builderCallbackCalled.ShouldBeTrue();
    [Fact] void should_have_the_correct_number_of_constraints() => _result.Count.ShouldEqual(1);
}
