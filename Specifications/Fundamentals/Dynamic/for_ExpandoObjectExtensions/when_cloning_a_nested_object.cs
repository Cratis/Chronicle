// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions
{
    public class when_cloning_a_nested_object : Specification
    {
        class complex_type { }

        ExpandoObject _original;
        ExpandoObject _clone;

        void Establish()
        {
            dynamic original = new ExpandoObject();
            original.Integer = 42;
            original.String = "Forty Two";
            original.Complex = new complex_type();
            dynamic nested = original.Nested = new ExpandoObject();
            nested.IntegerWithin = 43;
            nested.StringWithin = "Forty Three";
            _original = original;
        }

        void Because() => _clone = _original.Clone();

        [Fact] void should_create_new_instance() => _clone.ShouldNotBeSame(_original);
        [Fact] void should_copy_integer() => ((int)((dynamic)_clone).Integer).ShouldEqual(42);
        [Fact] void should_copy_string() => ((string)((dynamic)_clone).String).ShouldEqual("Forty Two");
        [Fact] void should_copy_nested_integer() => ((int)((dynamic)_clone).Nested.IntegerWithin).ShouldEqual(43);
        [Fact] void should_copy_nested_string() => ((string)((dynamic)_clone).Nested.StringWithin).ShouldEqual("Forty Three");
        [Fact] void should_copy_across_complex_type() => ((complex_type)((dynamic)_clone).Complex).ShouldBeSame((complex_type)((dynamic)_original).Complex);
    }
}
