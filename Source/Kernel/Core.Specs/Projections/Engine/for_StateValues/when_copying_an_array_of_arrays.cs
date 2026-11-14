// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_an_array_of_arrays : Specification
{
    object[][] _original;
    object _result;

    void Establish() => _original = [["a", "b"], ["c"]];

    void Because() => _result = StateValues.Copy(_original);

    [Fact] void should_return_a_list_of_object_arrays() => _result.ShouldBeOfExactType<List<object[]>>();
    [Fact] void should_return_another_inner_instance() => ((List<object[]>)_result)[0].ShouldNotBeSame(_original[0]);
    [Fact] void should_keep_the_inner_items_as_json_elements() => string.Join(',', ((List<object[]>)_result)[0].Select(_ => _.GetType().Name + ":" + _)).ShouldEqual("JsonElement:a,JsonElement:b");
    [Fact] void should_keep_the_outer_count() => ((List<object[]>)_result).Count.ShouldEqual(2);
}
