// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_an_expando_object_with_nested_collections : Specification
{
    ExpandoObject _original;
    ExpandoObject _result;
    List<object> _originalList;

    void Establish()
    {
        var item = new ExpandoObject();
        ((IDictionary<string, object?>)item)["name"] = "item";
        _originalList = [item];
        _original = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)_original;
        dictionary["items"] = _originalList;
        dictionary["lookup"] = new Dictionary<string, object> { ["list"] = new List<object> { "a" } };
    }

    void Because()
    {
        _result = (ExpandoObject)StateValues.Copy(_original);
        var copy = (IDictionary<string, object?>)_result;
        ((IDictionary<string, object?>)((List<object>)copy["items"]!)[0])["name"] = "changed";
        ((List<object>)((Dictionary<string, object>)copy["lookup"]!)["list"]).Add("b");
    }

    [Fact] void should_return_another_instance() => _result.ShouldNotBeSame(_original);
    [Fact] void should_copy_the_list() => ((IDictionary<string, object?>)_result)["items"].ShouldNotBeSame(_originalList);
    [Fact] void should_not_change_the_original_item() => ((IDictionary<string, object?>)_originalList[0])["name"].ShouldEqual("item");
    [Fact] void should_not_change_the_original_dictionary_list() => ((List<object>)((Dictionary<string, object>)((IDictionary<string, object?>)_original)["lookup"]!)["list"]).Count.ShouldEqual(1);
}
