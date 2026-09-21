// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

namespace Cratis.Chronicle.Projections.for_FromAllBuilder;

public class when_counting_into_a_dictionary_with_dynamic_key : Specification
{
    record ReadModel(Guid Id, Dictionary<string, int> EventCountByType);

    FromAllBuilder<ReadModel> _builder;
    Contracts.Projections.FromEveryDefinition _result;

    void Establish() => _builder = new(new TestNamingPolicy());

    void Because()
    {
        _builder.Count<int>(m => m.EventCountByType, c => c.EventType.Id);
        _result = _builder.Build();
    }

    [Fact] void should_have_one_property() => _result.Properties.Count.ShouldEqual(1);

    [Fact] void should_target_the_dictionary_property_with_the_dynamic_event_context_key() =>
        _result.Properties.ContainsKey("EventCountByType.$eventContext.EventType.Id").ShouldBeTrue();

    [Fact] void should_use_the_count_expression() =>
        _result.Properties["EventCountByType.$eventContext.EventType.Id"].ShouldEqual(WellKnownExpressions.Count);

    [Fact] void should_include_children() => _result.IncludeChildren.ShouldBeTrue();
}
