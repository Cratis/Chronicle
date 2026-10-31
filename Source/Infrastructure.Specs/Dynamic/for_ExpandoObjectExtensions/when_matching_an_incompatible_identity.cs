// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_matching_an_incompatible_identity : Specification
{
    ExpandoObject _child;
    ExpandoObject _key;
    bool _contains;
    ExpandoObject? _result;

    void Establish()
    {
        dynamic child = new ExpandoObject();
        child.id = 42;
        _child = child;
        dynamic key = new ExpandoObject();
        key.tenant = "tenant";
        key.number = 42;
        _key = key;
    }

    void Because()
    {
        ExpandoObject[] children = [_child];
        _contains = children.Contains(new PropertyPath("id"), _key);
        _result = children.GetItem(new PropertyPath("id"), _key);
    }

    [Fact] void should_not_match_the_identity() => _contains.ShouldBeFalse();
    [Fact] void should_not_select_a_child() => _result.ShouldBeNull();
}
