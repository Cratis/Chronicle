// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.MongoDB.for_ExpandoObjectConverter;

public record TargetType(
int IntValue,
float FloatValue,
double DoubleValue,
Guid GuidValue,
DateTime DateTimeValue,
DateTimeOffset DateTimeOffsetValue,
DateOnly DateOnlyValue,
TimeOnly TimeOnlyValue,
OtherType Reference,
IEnumerable<OtherType> Children);
