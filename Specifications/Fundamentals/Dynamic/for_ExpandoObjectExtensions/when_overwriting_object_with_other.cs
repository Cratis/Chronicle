// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_overwriting_object_with_other : Specification
{
    ExpandoObject left;
    dynamic left_as_dynamic;
    ExpandoObject right;
    dynamic right_as_dynamic;
    ExpandoObject result;
    dynamic result_as_dynamic;

    void Establish()
    {
        left_as_dynamic = left = new ExpandoObject();
        left_as_dynamic.Integer = 42;
        left_as_dynamic.String = "Forty Two";
        left_as_dynamic.Nested = new ExpandoObject();
        left_as_dynamic.Nested.Integer = 44;
        left_as_dynamic.Nested.String = "Forty Four";
        left_as_dynamic.LeftOnlyInteger = 46;
        left_as_dynamic.LeftOnlyString = "Forty Six";
        left_as_dynamic.PartialNested = new ExpandoObject();
        left_as_dynamic.PartialNested.Integer = 48;

        right_as_dynamic = right = new ExpandoObject();
        right_as_dynamic.Integer = 43;
        right_as_dynamic.String = "Forty Three";
        right_as_dynamic.Nested = new ExpandoObject();
        right_as_dynamic.Nested.Integer = 45;
        right_as_dynamic.Nested.String = "Forty Five";
        right_as_dynamic.RightOnlyInteger = 47;
        right_as_dynamic.RightOnlyString = "Forty Seven";
        right_as_dynamic.PartialNested = new ExpandoObject();
        right_as_dynamic.PartialNested.String = "Forty Eight";
        right_as_dynamic.RightOnlyNested = new ExpandoObject();
        right_as_dynamic.RightOnlyNested.Integer = 49;
        right_as_dynamic.RightOnlyNested.String = "Forty Nine";
    }

    void Because() => result_as_dynamic = result = left.MergeWith(right);

    [Fact] void should_create_a_new_object() => (result.GetHashCode() != left.GetHashCode() && result.GetHashCode() != right.GetHashCode()).ShouldBeTrue();
    [Fact] void should_overwrite_integer() => ((int)result_as_dynamic.Integer).ShouldEqual(43);
    [Fact] void should_overwrite_string() => ((string)result_as_dynamic.String).ShouldEqual("Forty Three");
    [Fact]
    void should_create_a_new_object_for_nested_object() => ((int)result_as_dynamic.Nested.GetHashCode() != (int)left_as_dynamic.Nested.GetHashCode() &&
                                                                        (int)result_as_dynamic.Nested.GetHashCode() != (int)right_as_dynamic.Nested.GetHashCode()).ShouldBeTrue();
    [Fact] void should_overwrite_nested_integer() => ((int)result_as_dynamic.Nested.Integer).ShouldEqual(45);
    [Fact] void should_overwrite_nested_string() => ((string)result_as_dynamic.Nested.String).ShouldEqual("Forty Five");
    [Fact] void should_maintain_left_only_integer() => ((int)result_as_dynamic.LeftOnlyInteger).ShouldEqual(46);
    [Fact] void should_maintain_left_only_string() => ((string)result_as_dynamic.LeftOnlyString).ShouldEqual("Forty Six");
    [Fact] void should_copy_right_only_integer() => ((int)result_as_dynamic.RightOnlyInteger).ShouldEqual(47);
    [Fact] void should_copy_right_only_string() => ((string)result_as_dynamic.RightOnlyString).ShouldEqual("Forty Seven");
    [Fact]
    void should_create_a_new_object_for_partial_nested_object() => ((int)result_as_dynamic.PartialNested.GetHashCode() != (int)left_as_dynamic.PartialNested.GetHashCode() &&
                                                                        (int)result_as_dynamic.PartialNested.GetHashCode() != (int)right_as_dynamic.PartialNested.GetHashCode()).ShouldBeTrue();
    [Fact] void should_maintain_left_partial_nested_integer() => ((int)result_as_dynamic.PartialNested.Integer).ShouldEqual(48);
    [Fact] void should_copy_right_partial_nested_string() => ((string)result_as_dynamic.PartialNested.String).ShouldEqual("Forty Eight");
    [Fact] void should_create_a_new_object_for_right_only_nested_object() => ((int)result_as_dynamic.RightOnlyNested.GetHashCode() != (int)right_as_dynamic.RightOnlyNested.GetHashCode()).ShouldBeTrue();
    [Fact] void should_copy_right_only_nested_integer() => ((int)result_as_dynamic.RightOnlyNested.Integer).ShouldEqual(49);
    [Fact] void should_copy_right_only_nested_string() => ((string)result_as_dynamic.RightOnlyNested.String).ShouldEqual("Forty Nine");
}
