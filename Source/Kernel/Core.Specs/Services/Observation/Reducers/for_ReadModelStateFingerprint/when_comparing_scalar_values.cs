// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReadModelStateFingerprint;

public class when_comparing_scalar_values : given.states
{
    static readonly Guid _guid = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001");
    static readonly DateTimeOffset _moment = new(2024, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact] void should_normalize_integral_types() => Of(5L).ShouldEqual(Of(5));
    [Fact] void should_normalize_integral_types_to_floating_point() => Of(5.0).ShouldEqual(Of(5));
    [Fact] void should_normalize_integral_decimals() => Of(5m).ShouldEqual(Of(5));
    [Fact] void should_normalize_trailing_zeros_of_decimals() => Of(1.50m).ShouldEqual(Of(1.5m));
    [Fact] void should_normalize_unsigned_types() => Of(7UL).ShouldEqual(Of((byte)7));
    [Fact] void should_tell_different_numbers_apart() => Of(5).ShouldNotEqual(Of(6));
    [Fact] void should_tell_different_fractions_apart() => Of(1.5).ShouldNotEqual(Of(1.25));
    [Fact] void should_tell_huge_unsigned_from_negative() => Of(ulong.MaxValue).ShouldNotEqual(Of(-1L));
    [Fact] void should_tell_a_number_from_its_text() => Of(5).ShouldNotEqual(Of("5"));
    [Fact] void should_tell_a_boolean_from_a_number() => Of(true).ShouldNotEqual(Of(1));
    [Fact] void should_tell_true_from_false() => Of(true).ShouldNotEqual(Of(false));
    [Fact] void should_tell_null_from_empty_text() => Of(null).ShouldNotEqual(Of(string.Empty));
    [Fact] void should_tell_different_text_apart() => Of("abc").ShouldNotEqual(Of("abd"));
    [Fact] void should_be_case_sensitive() => Of("abc").ShouldNotEqual(Of("ABC"));
    [Fact] void should_fingerprint_guids_by_their_text() => Of(_guid).ShouldEqual(Of(_guid.ToString("D")));
    [Fact] void should_fingerprint_enums_by_their_name() => Of(DayOfWeek.Friday).ShouldEqual(Of("Friday"));
    [Fact] void should_fingerprint_dates_by_their_invariant_text() => Of(_moment).ShouldEqual(Of(_moment.ToString("O")));
    [Fact] void should_tell_different_moments_apart() => Of(_moment).ShouldNotEqual(Of(_moment.AddSeconds(1)));
}
