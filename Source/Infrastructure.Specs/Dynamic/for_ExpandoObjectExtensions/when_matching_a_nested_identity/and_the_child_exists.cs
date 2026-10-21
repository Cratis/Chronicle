// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_matching_a_nested_identity;

public class and_the_child_exists : Specification
{
    ExpandoObject _child;
    ExpandoObject? _result;
    bool _contains;

    void Establish()
    {
        dynamic child = new ExpandoObject();
        child.identity = new ExpandoObject();
        child.identity.id = "A";
        _child = child;
    }

    void Because()
    {
        ExpandoObject[] children = [_child];
        _contains = children.Contains(new PropertyPath("identity.id"), "A");
        _result = children.GetItem(new PropertyPath("identity.id"), "A");
    }

    [Fact] void should_match_the_identity() => _contains.ShouldBeTrue();
    [Fact] void should_select_the_existing_child() => _result.ShouldEqual(_child);
}
