// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_generating_from_type;

public class with_nested_object_type : Specification
{
    record Address(string Street, string City);
    record PersonWithAddress(string Name, Address HomeAddress);

    JsonSchema _result;

    void Because() => _result = JsonSchema.FromType<PersonWithAddress>();

    [Fact] void should_set_title_on_root_type() => _result.Title.ShouldEqual(nameof(PersonWithAddress));
    [Fact] void should_set_title_on_nested_object_type() =>
        _result.ActualProperties["homeAddress"].ActualTypeSchema.Title.ShouldEqual(nameof(Address));
}
