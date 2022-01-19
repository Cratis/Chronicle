// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions
{
    public class when_converting_a_complex_nested_object_to_expando_object : Specification
    {
        ExpandoObject result;
        dynamic result_as_dynamic;

        void Because() => result_as_dynamic = result = new { Integer = 42, String = "Forty Two", Nested = new { Integer = 43, String = "Forty Three" } }.AsExpandoObject();

        [Fact] void should_create_an_expando_object() => result.ShouldNotBeNull();
        [Fact] void should_copy_integer_across() => ((int)result_as_dynamic.Integer).ShouldEqual(42);
        [Fact] void should_copy_string_across() => ((string)result_as_dynamic.String).ShouldEqual("Forty Two");
        [Fact] void should_create_expando_object_for_nested_object() => ((object)result_as_dynamic.Nested).ShouldBeOfExactType<ExpandoObject>();
        [Fact] void should_copy_nested_integer_across() => ((int)result_as_dynamic.Nested.Integer).ShouldEqual(43);
        [Fact] void should_copy_nested_string_across() => ((string)result_as_dynamic.Nested.String).ShouldEqual("Forty Three");
    }
}
