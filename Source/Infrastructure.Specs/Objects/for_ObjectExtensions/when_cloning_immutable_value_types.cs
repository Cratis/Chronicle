// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Objects.for_ObjectExtensions;

public class when_cloning_immutable_value_types : Specification
{
    static T RoundTrip<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    [Fact]
    void should_clone_utc_date_time_preserving_kind_and_ticks()
    {
        var value = new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).AddTicks(1234);
        var clone = value.Clone();
        clone.ShouldEqual(value);
        clone.Kind.ShouldEqual(DateTimeKind.Utc);
        clone.Ticks.ShouldEqual(value.Ticks);
        clone.ShouldEqual(RoundTrip(value));
    }

    [Fact]
    void should_clone_local_date_time_preserving_kind_and_ticks()
    {
        var value = new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Local).AddTicks(1234);
        var clone = value.Clone();
        clone.ShouldEqual(value);
        clone.Kind.ShouldEqual(DateTimeKind.Local);
        clone.Ticks.ShouldEqual(value.Ticks);
        clone.ShouldEqual(RoundTrip(value));
    }

    [Fact]
    void should_clone_unspecified_date_time_preserving_kind_and_ticks()
    {
        var value = new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Unspecified).AddTicks(1234);
        var clone = value.Clone();
        clone.ShouldEqual(value);
        clone.Kind.ShouldEqual(DateTimeKind.Unspecified);
        clone.Ticks.ShouldEqual(value.Ticks);
        clone.ShouldEqual(RoundTrip(value));
    }

    [Fact]
    void should_clone_date_time_offset_preserving_non_utc_offset()
    {
        var value = new DateTimeOffset(2020, 1, 2, 3, 4, 5, 678, TimeSpan.FromHours(5)).AddTicks(1234);
        var clone = value.Clone();
        clone.EqualsExact(value).ShouldBeTrue();
        clone.Offset.ShouldEqual(TimeSpan.FromHours(5));
        clone.EqualsExact(RoundTrip(value)).ShouldBeTrue();
    }

    [Fact]
    void should_clone_date_only()
    {
        var value = new DateOnly(2020, 7, 24);
        var clone = value.Clone();
        clone.ShouldEqual(value);
        clone.ShouldEqual(RoundTrip(value));
    }

    [Fact]
    void should_clone_time_only_preserving_sub_second_ticks()
    {
        var value = new TimeOnly(13, 14, 15, 678).Add(TimeSpan.FromTicks(1234));
        var clone = value.Clone();
        clone.ShouldEqual(value);
        clone.Ticks.ShouldEqual(value.Ticks);
        clone.ShouldEqual(RoundTrip(value));
    }

    [Fact]
    void should_clone_decimal_preserving_scale()
    {
        const decimal value = 1.00m;
        var clone = value.Clone();
        clone.ShouldEqual(value);
        decimal.GetBits(clone).ShouldEqual(decimal.GetBits(value));
        decimal.GetBits(clone).ShouldEqual(decimal.GetBits(RoundTrip(value)));
    }

    [Fact]
    void should_clone_enum_preserving_value_and_type()
    {
        const DayOfWeek value = DayOfWeek.Wednesday;
        var clone = value.Clone();
        clone.ShouldEqual(value);
        clone.GetType().ShouldEqual(typeof(DayOfWeek));
        clone.ShouldEqual(RoundTrip(value));
    }
}
