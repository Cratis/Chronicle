// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_a_list_with_mixed_items : Specification
{
    ExpandoObject _expando;
    List<object> _original;
    List<object> _result;
    object[] _arrayOriginal;
    List<object> _arrayResult;

    void Establish()
    {
        _expando = new ExpandoObject();
        ((IDictionary<string, object?>)_expando)["name"] = "item";
        _original = [1, "a", null!, _expando];
        _arrayOriginal = [1, "a", null!, _expando];
    }

    void Because()
    {
        _result = (List<object>)StateValues.Copy(_original);
        _arrayResult = (List<object>)StateValues.Copy(_arrayOriginal);
    }

    [Fact] void should_return_another_list() => _result.ShouldNotBeSame(_original);
    [Fact] void should_keep_the_item_count() => _result.Count.ShouldEqual(4);
    [Fact] void should_keep_the_number() => _result[0].ShouldEqual(1);
    [Fact] void should_keep_the_string() => _result[1].ShouldEqual("a");
    [Fact] void should_keep_the_null() => _result[2].ShouldBeNull();
    [Fact] void should_copy_the_expando_object() => _result[3].ShouldNotBeSame(_expando);
    [Fact] void should_keep_the_expando_content() => ((IDictionary<string, object?>)_result[3])["name"].ShouldEqual("item");
    [Fact] void should_copy_an_object_array_into_a_list_the_same_way() => _arrayResult.Count.ShouldEqual(4);
    [Fact] void should_copy_the_expando_object_in_an_array() => _arrayResult[3].ShouldNotBeSame(_expando);
}
