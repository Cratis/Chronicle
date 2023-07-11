// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_cloning_a_nested_object : Specification
{
    record complex_type(int AnInteger, string SomeString);

    ExpandoObject original;
    ExpandoObject clone;
    ExpandoObject child;

    dynamic original_dynamic;
    dynamic clone_dynamic;

    void Establish()
    {
        dynamic expando = new ExpandoObject();
        expando.Integer = 42;
        expando.String = "Forty Two";
        expando.Complex = new complex_type(45, "Forty five");
        dynamic nested = expando.Nested = new ExpandoObject();
        nested.IntegerWithin = 43;
        nested.StringWithin = "Forty Three";

        dynamic childExpando = new ExpandoObject();
        childExpando.Integer = 44;
        childExpando.String = "Forty Four";
        child = childExpando;

        expando.Collection = new ExpandoObject[] { childExpando };

        original = expando;
    }

    void Because()
    {
        original_dynamic = original;
        clone = original.Clone();
        clone_dynamic = clone;
    }

    [Fact] void should_create_new_instance() => clone.ShouldNotBeSame(original);
    [Fact] void should_create_new_instance_for_nested() => ((ExpandoObject)((dynamic)clone).Nested).GetHashCode().ShouldNotEqual(((ExpandoObject)((dynamic)original).Nested).GetHashCode());
    [Fact] void should_copy_integer() => ((int)((dynamic)clone).Integer).ShouldEqual(42);
    [Fact] void should_copy_string() => ((string)((dynamic)clone).String).ShouldEqual("Forty Two");
    [Fact] void should_copy_nested_integer() => ((int)((dynamic)clone).Nested.IntegerWithin).ShouldEqual(43);
    [Fact] void should_copy_nested_string() => ((string)((dynamic)clone).Nested.StringWithin).ShouldEqual("Forty Three");
    [Fact] void should_create_new_instance_for_complex_type() => ((complex_type)clone_dynamic.Complex).ShouldNotBeSame((complex_type)original_dynamic.Complex);
    [Fact] void should_copy_complex_types_integer() => ((complex_type)clone_dynamic.Complex).AnInteger.ShouldEqual(((complex_type)original_dynamic.Complex).AnInteger);
    [Fact] void should_copy_complex_types_string() => ((complex_type)clone_dynamic.Complex).SomeString.ShouldEqual(((complex_type)original_dynamic.Complex).SomeString);
    [Fact] void should_create_new_instance_for_child() => ((IEnumerable<ExpandoObject>)((dynamic)clone).Collection).First().ShouldNotBeSame(((IEnumerable<ExpandoObject>)((dynamic)original).Collection).First());
    [Fact] void should_copy_child_integer() => ((int)((dynamic)((IEnumerable<ExpandoObject>)clone_dynamic.Collection).First()).Integer).ShouldEqual(44);
    [Fact] void should_copy_child_string() => ((string)((dynamic)((IEnumerable<ExpandoObject>)clone_dynamic.Collection).First()).String).ShouldEqual("Forty Four");
}
