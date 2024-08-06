// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Strings;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder;

public class when_building_a_unique_constraint : given.a_constraint_builder_with_owner
{
    IImmutableList<IConstraintDefinition> _result;
    bool _builderCallbackCalled;

    void Because()
    {
        var eventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        _eventTypes.GetSchemaFor(eventType.Id).Returns(_generator.Generate(typeof(EventWithStringProperty)));
        _constraintBuilder.Unique(_ =>
        {
            _.On(eventType, nameof(EventWithStringProperty.SomeProperty).ToCamelCase());
            _builderCallbackCalled = true;
        });
        _result = _constraintBuilder.Build();
    }

    [Fact] void should_call_the_builder_callback() => _builderCallbackCalled.ShouldBeTrue();
    [Fact] void should_have_the_correct_number_of_constraints() => _result.Count.ShouldEqual(1);
}
