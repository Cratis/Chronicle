// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_a_dictionary_holding_a_list : Specification
{
    Dictionary<string, object> _original;
    List<object> _originalList;
    Dictionary<string, object> _result;

    void Establish()
    {
        _originalList = ["one"];
        _original = new() { ["list"] = _originalList };
    }

    void Because()
    {
        _result = (Dictionary<string, object>)StateValues.Copy(_original);
        ((List<object>)_result["list"]).Add("two");
    }

    [Fact] void should_return_another_dictionary() => _result.ShouldNotBeSame(_original);
    [Fact] void should_copy_the_list() => _result["list"].ShouldNotBeSame(_originalList);
    [Fact] void should_not_change_the_original_list() => _originalList.Count.ShouldEqual(1);
}
