// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Aksio.Cratis.Json.for_ExpandoObjectConverter;

public enum AnEnumValue
{
    First = 1,
    Second = 2
}

public enum AnEnumWithStringValues
{
    [EnumMember(Value = "Undefined")]
    Undefined = 0,

    [EnumMember(Value = "First")]
    First = 1,

    [EnumMember(Value = "Second")]
    Second = 2,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AnEnumWithStringValuesAndJsonConverter
{
    [EnumMember(Value = "Undefined")]
    Undefined = 0,

    [EnumMember(Value = "First")]
    First = 1,

    [EnumMember(Value = "Second")]
    Second = 2,
}

public record TargetType(
    int IntValue,
    float FloatValue,
    double DoubleValue,
    AnEnumValue EnumValue,
    AnEnumValue? NullableEnumValue,
    AnEnumValue? NullableEnumValueSetToNull,
    AnEnumWithStringValues EnumAsStringValueRepresentedAsInt,
    AnEnumWithStringValues EnumAsStringValue,
    AnEnumWithStringValuesAndJsonConverter EnumAsStringValueWithJsonConverter,
    Guid GuidValue,
    DateTime DateTimeValue,
    DateTimeOffset DateTimeOffsetValue,
    DateOnly DateOnlyValue,
    TimeOnly TimeOnlyValue,
    OtherType Reference,
    IEnumerable<OtherType> Children,
    IEnumerable<string> StringArray,
    IEnumerable<int> IntArray,
    IDictionary<string, string> StringDictionary,
    IDictionary<string, int> IntDictionary,
    IDictionary<string, OtherType> ComplexTypeDictionary,
    string MissingStringFromSource,
    int MissingIntFromSource);
