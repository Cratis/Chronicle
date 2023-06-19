// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.MongoDB.for_ExpandoObjectConverter;

public enum AnEnumValue
{
    First = 1,
    Second = 2,
}

public record TargetType(
    int IntValue,
    float FloatValue,
    double DoubleValue,
    AnEnumValue EnumValue,
    Guid GuidValue,
    DateTime DateTimeValue,
    DateTimeOffset DateTimeOffsetValue,
    DateOnly DateOnlyValue,
    TimeOnly TimeOnlyValue,
    OtherType Reference,
    IEnumerable<OtherType> Children);
