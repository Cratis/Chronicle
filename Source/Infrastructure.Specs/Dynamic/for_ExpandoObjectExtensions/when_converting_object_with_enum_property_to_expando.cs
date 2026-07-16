// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_converting_object_with_enum_property_to_expando : Specification
{
    public enum Status
    {
        Pending = 0,
        Active = 1,
        Archived = 2
    }

    public record SomethingWithStatus(string Name, Status Status);

    SomethingWithStatus _source;
    ExpandoObject _result;
    object _statusValue;

    void Establish() => _source = new("Example", Status.Active);

    void Because()
    {
        _result = _source.AsExpandoObject();
        _statusValue = ((IDictionary<string, object?>)_result)["Status"];
    }

    [Fact] void should_store_enum_value_as_the_enum_type_not_a_nested_expando() =>
        _statusValue.ShouldBeOfExactType<Status>();

    [Fact] void should_preserve_the_enum_value() =>
        ((Status)_statusValue).ShouldEqual(Status.Active);
}
