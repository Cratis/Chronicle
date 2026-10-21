// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_matching_a_nested_identity;

public class and_the_child_does_not_exist : Specification
{
    ExpandoObject _child;
    ExpandoObject? _result;
    bool _contains;

    void Establish() => _child = new ExpandoObject();

    void Because()
    {
        ExpandoObject[] children = [_child];
        _contains = children.Contains(new PropertyPath("identity.id"), "A");
        _result = children.GetItem(new PropertyPath("identity.id"), "A");
    }

    [Fact] void should_not_match_the_identity() => _contains.ShouldBeFalse();
    [Fact] void should_not_select_a_child() => _result.ShouldBeNull();
    [Fact] void should_not_create_the_missing_identity() => _child.ShouldBeEmpty();
}
