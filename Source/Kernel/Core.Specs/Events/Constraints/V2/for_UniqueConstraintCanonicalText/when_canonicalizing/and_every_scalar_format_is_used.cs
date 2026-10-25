// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintCanonicalText.when_canonicalizing;

public class and_every_scalar_format_is_used : Specification
{
    Dictionary<string, string> _result;

    void Because()
    {
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new("tr-TR");
            CultureInfo.CurrentUICulture = new("tr-TR");
            _result = Inputs().ToDictionary(_ => _.Key, _ => UniqueConstraintCanonicalText.For(_.Value.Value!, _.Value.Format, _.Value.IsInSchema, false));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    Dictionary<string, UniqueConstraintComponent> Inputs()
    {
        var instant = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc).AddTicks(1234567);
        var guid = new Guid("abcdef01-2345-6789-abcd-ef0123456789");
        return new()
        {
            ["guid"] = new(guid, "guid"),
            ["uuid"] = new(guid, "uuid"),
            ["int16"] = new(short.MinValue, "int16"),
            ["int32"] = new(int.MinValue, "int32"),
            ["int64"] = new(long.MinValue, "int64"),
            ["byte"] = new(byte.MaxValue, "byte"),
            ["uint32"] = new(uint.MaxValue, "uint32"),
            ["uint64"] = new(ulong.MaxValue, "uint64"),
            ["float"] = new(1.25f, "float"),
            ["double"] = new(1.25d, "double"),
            ["float-negative-zero"] = new(-0.0f, "float"),
            ["double-negative-zero"] = new(-0.0d, "double"),
            ["float-exponent"] = new(1E+20f, "float"),
            ["double-exponent"] = new(1E+100d, "double"),
            ["decimal"] = new(1.2300m, "decimal"),
            ["decimal-negative-zero"] = new(new decimal(0, 0, 0, true, 28), "decimal"),
            ["decimal-max"] = new(decimal.MaxValue, "decimal"),
            ["decimal-min"] = new(decimal.MinValue, "decimal"),
            ["decimal-small"] = new(0.0000000000000000000000000001m, "decimal"),
            ["date-time"] = new(instant, "date-time"),
            ["date-time-unspecified"] = new(DateTime.SpecifyKind(instant, DateTimeKind.Unspecified), "date-time"),
            ["date-time-local"] = new(instant.ToLocalTime(), "date-time"),
            ["date-time-offset"] = new(new DateTimeOffset(instant).ToOffset(TimeSpan.FromHours(2)), "date-time-offset"),
            ["date"] = new(new DateOnly(2025, 2, 3), "date"),
            ["time"] = new(new TimeOnly(4, 5, 6).Add(TimeSpan.FromTicks(1234567)), "time"),
            ["duration"] = new(TimeSpan.FromDays(1) + TimeSpan.FromTicks(1234567), "duration"),
            ["duration-negative"] = new(TimeSpan.FromHours(-1), "duration"),
            ["true"] = new(true),
            ["false"] = new(false),
            ["string"] = new("Ié-e\u0301"),
            ["empty-format"] = new("I", string.Empty),
            ["schema-guid-string"] = new("{ABCDEF01-2345-6789-ABCD-EF0123456789}"),
            ["unknown-guid"] = new("{ABCDEF01-2345-6789-ABCD-EF0123456789}", IsInSchema: false),
            ["unknown-guid-compact"] = new("ABCDEF0123456789ABCDEF0123456789", IsInSchema: false),
            ["unknown-string"] = new("I-not-a-guid", IsInSchema: false),
            ["unknown-number"] = new(1.25m, IsInSchema: false),
            ["unknown-large-integer"] = new(9007199254740993L, IsInSchema: false),
            ["unknown-boolean"] = new(true, IsInSchema: false),
            ["number"] = new(1.25d),
            ["number-negative-zero"] = new(-0.0d)
        };
    }

    [Fact] void should_format_guid() => _result["guid"].ShouldEqual("abcdef01-2345-6789-abcd-ef0123456789");
    [Fact] void should_format_uuid_alias() => _result["uuid"].ShouldEqual(_result["guid"]);
    [Fact] void should_format_int16() => _result["int16"].ShouldEqual("-32768");
    [Fact] void should_format_int32() => _result["int32"].ShouldEqual("-2147483648");
    [Fact] void should_format_int64() => _result["int64"].ShouldEqual("-9223372036854775808");
    [Fact] void should_format_byte() => _result["byte"].ShouldEqual("255");
    [Fact] void should_format_uint32() => _result["uint32"].ShouldEqual("4294967295");
    [Fact] void should_format_uint64_without_losing_precision() => _result["uint64"].ShouldEqual("18446744073709551615");
    [Fact] void should_format_float() => _result["float"].ShouldEqual("1.25");
    [Fact] void should_format_double() => _result["double"].ShouldEqual("1.25");
    [Fact] void should_collapse_float_negative_zero() => _result["float-negative-zero"].ShouldEqual("0");
    [Fact] void should_collapse_double_negative_zero() => _result["double-negative-zero"].ShouldEqual("0");
    [Fact] void should_round_trip_float_exponents() => _result["float-exponent"].ShouldEqual("1E+20");
    [Fact] void should_round_trip_double_exponents() => _result["double-exponent"].ShouldEqual("1E+100");
    [Fact] void should_remove_decimal_trailing_zeros() => _result["decimal"].ShouldEqual("1.23");
    [Fact] void should_collapse_decimal_negative_zero() => _result["decimal-negative-zero"].ShouldEqual("0");
    [Fact] void should_keep_maximum_decimal_precision() => _result["decimal-max"].ShouldEqual("79228162514264337593543950335");
    [Fact] void should_keep_minimum_decimal_precision() => _result["decimal-min"].ShouldEqual("-79228162514264337593543950335");
    [Fact] void should_never_use_decimal_exponents() => _result["decimal-small"].ShouldEqual("0.0000000000000000000000000001");
    [Fact] void should_format_utc_date_time() => _result["date-time"].ShouldEqual("2025-02-03T04:05:06.1234567Z");
    [Fact] void should_treat_unspecified_kind_as_utc() => _result["date-time-unspecified"].ShouldEqual(_result["date-time"]);
    [Fact] void should_convert_local_kind_to_utc() => _result["date-time-local"].ShouldEqual(_result["date-time"]);
    [Fact] void should_convert_offset_to_utc() => _result["date-time-offset"].ShouldEqual(_result["date-time"]);
    [Fact] void should_format_date() => _result["date"].ShouldEqual("2025-02-03");
    [Fact] void should_format_time_without_truncating_clr_ticks() => _result["time"].ShouldEqual("04:05:06.1234567");
    [Fact] void should_format_duration() => _result["duration"].ShouldEqual("1.00:00:00.1234567");
    [Fact] void should_format_negative_duration() => _result["duration-negative"].ShouldEqual("-01:00:00");
    [Fact] void should_format_true() => _result["true"].ShouldEqual("true");
    [Fact] void should_format_false() => _result["false"].ShouldEqual("false");
    [Fact] void should_preserve_unicode_without_normalization() => _result["string"].ShouldEqual("Ié-e\u0301");
    [Fact] void should_accept_an_empty_schema_format() => _result["empty-format"].ShouldEqual("I");
    [Fact] void should_not_parse_a_schema_declared_string_as_guid() => _result["schema-guid-string"].ShouldEqual("{ABCDEF01-2345-6789-ABCD-EF0123456789}");
    [Fact] void should_normalize_unknown_guid_strings() => _result["unknown-guid"].ShouldEqual(_result["guid"]);
    [Fact] void should_normalize_compact_unknown_guid_strings() => _result["unknown-guid-compact"].ShouldEqual(_result["guid"]);
    [Fact] void should_preserve_other_unknown_strings() => _result["unknown-string"].ShouldEqual("I-not-a-guid");
    [Fact] void should_treat_unknown_numbers_as_double() => _result["unknown-number"].ShouldEqual("1.25");
    [Fact] void should_use_double_precision_for_unknown_integers() => _result["unknown-large-integer"].ShouldEqual("9007199254740992");
    [Fact] void should_format_unknown_booleans() => _result["unknown-boolean"].ShouldEqual("true");
    [Fact] void should_format_formatless_numbers_as_double() => _result["number"].ShouldEqual("1.25");
    [Fact] void should_collapse_formatless_negative_zero() => _result["number-negative-zero"].ShouldEqual("0");
}
