// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_a_list_of_expando_objects : Specification
{
    List<object> _original;
    List<object> _result;
    ExpandoObject _originalItem;

    void Establish()
    {
        var inner = new ExpandoObject();
        ((IDictionary<string, object?>)inner)["leaf"] = "leaf-value";
        _originalItem = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)_originalItem;
        dictionary["name"] = "first";
        dictionary["inner"] = inner;
        _original = [_originalItem];
    }

    void Because()
    {
        _result = (List<object>)StateValues.Copy(_original);
        var copiedItem = (IDictionary<string, object?>)_result[0];
        copiedItem["name"] = "changed";
        ((IDictionary<string, object?>)copiedItem["inner"]!)["leaf"] = "changed";
        _result.Add("extra");
    }

    [Fact] void should_return_another_list() => _result.ShouldNotBeSame(_original);
    [Fact] void should_return_another_item_instance() => _result[0].ShouldNotBeSame(_originalItem);
    [Fact] void should_not_change_the_original_list() => _original.Count.ShouldEqual(1);
    [Fact] void should_not_change_the_original_item() => ((IDictionary<string, object?>)_originalItem)["name"].ShouldEqual("first");
    [Fact] void should_not_change_the_original_nested_item() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_originalItem)["inner"]!)["leaf"].ShouldEqual("leaf-value");
}
