// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_matching_a_composite_identity : Specification
{
    ExpandoObject _child;
    ExpandoObject _key;
    bool _contains;
    ExpandoObject? _result;

    void Establish()
    {
        dynamic storedKey = new ExpandoObject();
        storedKey.tenant = "tenant";
        storedKey.number = 42;
        dynamic child = new ExpandoObject();
        child.id = storedKey;
        _child = child;
        dynamic key = new ExpandoObject();
        key.number = 42;
        key.tenant = "tenant";
        _key = key;
    }

    void Because()
    {
        ExpandoObject[] children = [_child];
        _contains = children.Contains(new PropertyPath("id"), _key);
        _result = children.GetItem(new PropertyPath("id"), _key);
    }

    [Fact] void should_match_an_independently_materialized_composite_key() => _contains.ShouldBeTrue();
    [Fact] void should_select_the_existing_child() => _result.ShouldBeSame(_child);
}
