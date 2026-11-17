// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_merging_expando_objects : Specification
{
    ExpandoObject _left;
    ExpandoObject _right;
    ExpandoObject _rightNested;
    IDictionary<string, object?> _result;

    void Establish()
    {
        _left = new ExpandoObject();
        var left = (IDictionary<string, object?>)_left;
        left["keep"] = "left";
        left["overwrite"] = "left";
        left["merged"] = Expando(("a", "left"), ("b", "left"));
        left["replaced"] = "scalar";

        _rightNested = Expando(("x", 1));
        _right = new ExpandoObject();
        var right = (IDictionary<string, object?>)_right;
        right["overwrite"] = "right";
        right["merged"] = Expando(("b", "right"), ("c", "right"));
        right["replaced"] = Expando(("y", 2));
        right["cloned"] = _rightNested;
        right["added"] = "added";
    }

    void Because() => _result = _left.MergeWith(_right);

    [Fact] void should_keep_left_only_values() => _result["keep"].ShouldEqual("left");
    [Fact] void should_overwrite_with_right_values() => _result["overwrite"].ShouldEqual("right");
    [Fact] void should_add_right_only_values() => _result["added"].ShouldEqual("added");
    [Fact] void should_merge_nested_left_values() => ((IDictionary<string, object?>)_result["merged"]!)["a"].ShouldEqual("left");
    [Fact] void should_merge_nested_overwriting_values() => ((IDictionary<string, object?>)_result["merged"]!)["b"].ShouldEqual("right");
    [Fact] void should_merge_nested_right_values() => ((IDictionary<string, object?>)_result["merged"]!)["c"].ShouldEqual("right");
    [Fact] void should_replace_non_expando_left_value_with_right_expando() => ((IDictionary<string, object?>)_result["replaced"]!)["y"].ShouldEqual(2);
    [Fact] void should_clone_right_expando_values() => ReferenceEquals(_result["cloned"], _rightNested).ShouldBeFalse();
    [Fact] void should_keep_cloned_content() => ((IDictionary<string, object?>)_result["cloned"]!)["x"].ShouldEqual(1);
    [Fact] void should_not_modify_left() => ((IDictionary<string, object?>)_left)["overwrite"].ShouldEqual("left");
    [Fact] void should_not_modify_left_nested() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_left)["merged"]!).ContainsKey("c").ShouldBeFalse();

    static ExpandoObject Expando(params (string Key, object Value)[] values)
    {
        var expando = new ExpandoObject();
        foreach (var (key, value) in values) ((IDictionary<string, object?>)expando)[key] = value;
        return expando;
    }
}
