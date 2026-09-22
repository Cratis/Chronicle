// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_CompositeKeyBuilder.when_setting_a_key_part;

public class and_accessor_is_an_iso_week_derived_function : Specification
{
    public record KeyType(int Year, int Week);
    public record SomeEvent(string Name);

    string _result;

    void Because()
    {
        var builder = new CompositeKeyBuilder<KeyType, SomeEvent>(new DefaultNamingPolicy());
        builder.Set(x => x.Year).ToEventContextProperty(c => c.Occurred.Year);
        builder.Set(x => x.Week).ToEventContextProperty(c => c.Occurred.Week());
        _result = builder.Build();
    }

    [Fact] void should_build_a_composite_key_with_the_year_and_the_iso_week() =>
        _result.ShouldEqual($"{Cratis.Chronicle.WellKnownExpressions.Composite}(Year={Cratis.Chronicle.WellKnownExpressions.EventContext}(Occurred.Year),Week={Cratis.Chronicle.WellKnownExpressions.EventContext}(Occurred.Week))");
}
