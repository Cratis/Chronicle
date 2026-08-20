// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_TypeHelper.when_getting_type_name;

public class for_datetime_offset : Specification
{
    string _typeName = string.Empty;
    string _nullableTypeName = string.Empty;

    void Because()
    {
        _typeName = TypeHelper.GetTypeName(typeof(DateTimeOffset));
        _nullableTypeName = TypeHelper.GetTypeName(typeof(DateTimeOffset?));
    }

    [Fact] void should_use_the_serializable_date_time_offset_contract() =>
        _typeName.ShouldEqual("Cratis.Chronicle.Contracts.Primitives.SerializableDateTimeOffset");
    [Fact] void should_represent_nullable_values_with_the_reference_type() =>
        _nullableTypeName.ShouldEqual("Cratis.Chronicle.Contracts.Primitives.SerializableDateTimeOffset");
}
