// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_generating_from_type;

public class with_all_known_format_types : Specification
{
    record TypeWithAllFormats(
        short AShort,
        int AnInt,
        uint AUint,
        long ALong,
        ulong AUlong,
        float AFloat,
        double ADouble,
        decimal ADecimal,
        byte AByte,
        DateTime ADateTime,
        DateTimeOffset ADateTimeOffset,
        DateOnly ADateOnly,
        TimeOnly ATimeOnly,
        TimeSpan ATimeSpan,
        Guid AGuid);

    JsonSchema _result;

    void Because() => _result = JsonSchema.FromType<TypeWithAllFormats>();

    [Fact] void should_inject_int16_format_for_short() => _result.ActualProperties["aShort"].Format.ShouldEqual("int16");
    [Fact] void should_inject_int32_format_for_int() => _result.ActualProperties["anInt"].Format.ShouldEqual("int32");
    [Fact] void should_inject_uint32_format_for_uint() => _result.ActualProperties["aUint"].Format.ShouldEqual("uint32");
    [Fact] void should_inject_int64_format_for_long() => _result.ActualProperties["aLong"].Format.ShouldEqual("int64");
    [Fact] void should_inject_uint64_format_for_ulong() => _result.ActualProperties["aUlong"].Format.ShouldEqual("uint64");
    [Fact] void should_inject_float_format_for_float() => _result.ActualProperties["aFloat"].Format.ShouldEqual("float");
    [Fact] void should_inject_double_format_for_double() => _result.ActualProperties["aDouble"].Format.ShouldEqual("double");
    [Fact] void should_inject_decimal_format_for_decimal() => _result.ActualProperties["aDecimal"].Format.ShouldEqual("decimal");
    [Fact] void should_inject_byte_format_for_byte() => _result.ActualProperties["aByte"].Format.ShouldEqual("byte");
    [Fact] void should_inject_date_time_format_for_date_time() => _result.ActualProperties["aDateTime"].Format.ShouldEqual("date-time");
    [Fact] void should_inject_date_time_offset_format_for_date_time_offset() => _result.ActualProperties["aDateTimeOffset"].Format.ShouldEqual("date-time-offset");
    [Fact] void should_inject_date_format_for_date_only() => _result.ActualProperties["aDateOnly"].Format.ShouldEqual("date");
    [Fact] void should_inject_time_format_for_time_only() => _result.ActualProperties["aTimeOnly"].Format.ShouldEqual("time");
    [Fact] void should_inject_duration_format_for_time_span() => _result.ActualProperties["aTimeSpan"].Format.ShouldEqual("duration");
    [Fact] void should_inject_guid_format_for_guid() => _result.ActualProperties["aGuid"].Format.ShouldEqual("guid");
}
