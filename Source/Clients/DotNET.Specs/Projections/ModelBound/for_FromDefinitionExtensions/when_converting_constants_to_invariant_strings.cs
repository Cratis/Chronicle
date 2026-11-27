// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_FromDefinitionExtensions;

public class when_converting_constants_to_invariant_strings : Specification
{
    string[] _results;

    enum IntValue
    {
        Selected = 7
    }

    enum LongValue : long
    {
        Selected = 7
    }

    void Because()
    {
        object[] values = [IntValue.Selected, LongValue.Selected, 12.5m, new DateOnly(2026, 1, 2)];
        _results = values.Select(FromDefinitionExtensions.ConvertValueToInvariantString).ToArray();
    }

    [Fact] void should_use_the_number_for_an_int_backed_enum() => _results[0].ShouldEqual("7");
    [Fact] void should_keep_the_name_for_other_enum_backings() => _results[1].ShouldEqual("Selected");
    [Fact] void should_use_an_invariant_decimal_separator() => _results[2].ShouldEqual("12.5");
    [Fact] void should_use_the_round_trip_date_format() => _results[3].ShouldEqual("2026-01-02");
}
