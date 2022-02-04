// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions
{
    public class when_cloning_a_nested_object : Specification
    {
        class complex_type { }

        ExpandoObject original;
        ExpandoObject clone;

        void Establish()
        {
            dynamic expando = new ExpandoObject();
            expando.Integer = 42;
            expando.String = "Forty Two";
            expando.Complex = new complex_type();
            dynamic nested = expando.Nested = new ExpandoObject();
            nested.IntegerWithin = 43;
            nested.StringWithin = "Forty Three";
            original = expando;
        }

        void Because() => clone = original.Clone();

        [Fact] void should_create_new_instance() => clone.ShouldNotBeSame(original);
        [Fact] void should_copy_integer() => ((int)((dynamic)clone).Integer).ShouldEqual(42);
        [Fact] void should_copy_string() => ((string)((dynamic)clone).String).ShouldEqual("Forty Two");
        [Fact] void should_copy_nested_integer() => ((int)((dynamic)clone).Nested.IntegerWithin).ShouldEqual(43);
        [Fact] void should_copy_nested_string() => ((string)((dynamic)clone).Nested.StringWithin).ShouldEqual("Forty Three");
        [Fact] void should_copy_across_complex_type() => ((complex_type)((dynamic)clone).Complex).ShouldBeSame((complex_type)((dynamic)original).Complex);
    }
}
