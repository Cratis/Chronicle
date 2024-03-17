// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_StringExtensions;

public class when_converting_a_string_representation_of_a_guid_to_a_guid : Specification
{
    static string guid_as_a_string;
    static Guid result;

    void Establish() => guid_as_a_string = Guid.NewGuid().ToString();

    void Because() => result = (Guid)guid_as_a_string.ParseTo(typeof(Guid));

    [Fact] void should_create_a_guid() => result.ShouldBeOfExactType<Guid>();
    [Fact] void should_have_the_correct_value() => result.ToString().ShouldEqual(guid_as_a_string);
}
