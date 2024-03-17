// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.for_StringExtensions;

public class when_converting_a_string_representation_of_a_long_to_a_long : Specification
{
    static string long_as_a_string;
    static long result;

    void Establish() => long_as_a_string = "7654321";

    void Because() => result = (long)long_as_a_string.ParseTo(typeof(long));

    [Fact] void should_create_a_long() => result.ShouldBeOfExactType<long>();
    [Fact] void should_have_the_correct_value() => result.ToString().ShouldEqual(long_as_a_string);
}
