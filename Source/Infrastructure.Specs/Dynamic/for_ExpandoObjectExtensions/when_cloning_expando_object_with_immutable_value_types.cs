// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_cloning_expando_object_with_immutable_value_types : Specification
{
    ExpandoObject _original;
    IDictionary<string, object?> _clone;

    DateTime _dateTime;
    DateTimeOffset _dateTimeOffset;
    DateOnly _dateOnly;
    TimeOnly _timeOnly;
    decimal _decimal;
    DayOfWeek _enum;

    void Establish()
    {
        _dateTime = new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Unspecified).AddTicks(1234);
        _dateTimeOffset = new DateTimeOffset(2020, 1, 2, 3, 4, 5, 678, TimeSpan.FromHours(5)).AddTicks(1234);
        _dateOnly = new DateOnly(2020, 7, 24);
        _timeOnly = new TimeOnly(13, 14, 15, 678).Add(TimeSpan.FromTicks(1234));
        _decimal = 1.00m;
        _enum = DayOfWeek.Wednesday;

        _original = new();
        dynamic asDynamic = _original;
        asDynamic.DateTime = _dateTime;
        asDynamic.DateTimeOffset = _dateTimeOffset;
        asDynamic.DateOnly = _dateOnly;
        asDynamic.TimeOnly = _timeOnly;
        asDynamic.Decimal = _decimal;
        asDynamic.Enum = _enum;
    }

    void Because() => _clone = _original.Clone();

    [Fact] void should_preserve_date_time_kind_and_ticks() => ((DateTime)_clone["DateTime"]!).ShouldEqual(_dateTime);
    [Fact] void should_preserve_date_time_kind() => ((DateTime)_clone["DateTime"]!).Kind.ShouldEqual(DateTimeKind.Unspecified);
    [Fact] void should_preserve_date_time_offset_exactly() => ((DateTimeOffset)_clone["DateTimeOffset"]!).EqualsExact(_dateTimeOffset).ShouldBeTrue();
    [Fact] void should_preserve_date_only() => ((DateOnly)_clone["DateOnly"]!).ShouldEqual(_dateOnly);
    [Fact] void should_preserve_time_only_ticks() => ((TimeOnly)_clone["TimeOnly"]!).Ticks.ShouldEqual(_timeOnly.Ticks);
    [Fact] void should_preserve_decimal_scale() => decimal.GetBits((decimal)_clone["Decimal"]!).ShouldEqual(decimal.GetBits(_decimal));
    [Fact] void should_preserve_enum_value() => ((DayOfWeek)_clone["Enum"]!).ShouldEqual(_enum);
}
