// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_the_culture_is_different : Specification
{
    CultureInfo _original;
    string _invariant;
    string _german;
    string _turkish;

    void Establish() => _original = CultureInfo.CurrentCulture;

    void Because()
    {
        _invariant = Encode(CultureInfo.InvariantCulture);
        _german = Encode(CommaCulture());
        _turkish = Encode(GroupingCulture());
    }

    void Destroy() => CultureInfo.CurrentCulture = _original;

    [Fact] void should_give_the_same_value_for_german() => _german.ShouldEqual(_invariant);
    [Fact] void should_give_the_same_value_for_other_separators() => _turkish.ShouldEqual(_invariant);
    [Fact] void should_format_the_decimal_with_a_point() => _invariant.ShouldContain("3:1.5");

    static CultureInfo CommaCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NegativeSign = "~";
        return culture;
    }

    static CultureInfo GroupingCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberGroupSeparator = ".";
        culture.DateTimeFormat.DateSeparator = "/";
        culture.DateTimeFormat.TimeSeparator = ".";
        return culture;
    }

    static string Encode(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        return CompositeKeyEncoding.Encode(new Dictionary<string, object?>
        {
            ["amount"] = 1.5m,
            ["count"] = 1234567,
            ["day"] = new DateTime(2026, 10, 23, 8, 0, 0, DateTimeKind.Utc),
            ["id"] = "I"
        });
    }
}
