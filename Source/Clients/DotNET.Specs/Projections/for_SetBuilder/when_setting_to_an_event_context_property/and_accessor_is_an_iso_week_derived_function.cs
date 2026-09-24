// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_SetBuilder.when_setting_to_an_event_context_property;

public class and_accessor_is_an_iso_week_derived_function : Specification
{
    public record SomeEvent(string Name);
    public record ReadModel(int Week);

    SetBuilder<ReadModel, SomeEvent, int, object> _builder;
    Exception? _error;
    string _result;

    void Establish() => _builder = new SetBuilder<ReadModel, SomeEvent, int, object>(new object(), new PropertyPath("Week"), new DefaultNamingPolicy());

    void Because()
    {
        _error = Catch.Exception(() => _builder.ToEventContextProperty(c => c.Occurred.Week()));
        _result = _builder.Build();
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_build_an_event_context_expression_rendering_the_derived_property_without_parens() => _result.ShouldEqual($"{Cratis.Chronicle.WellKnownExpressions.EventContext}(Occurred.Week)");
}
