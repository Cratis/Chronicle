// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage.MongoDB.for_ExpandoObjectConverter;

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
    IEnumerable<int> IntChildren,
    IEnumerable<string> StringChildren,
    IEnumerable<OtherType> Children,
    IDictionary<string, string> StringDictionary,
    IDictionary<string, int> IntDictionary,
    IDictionary<string, OtherType> ComplexTypeDictionary);
