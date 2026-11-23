// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_finding_an_object_by_a_nested_composite_identity : Specification
{
    ExpandoObject _child;
    ExpandoObject _key;
    ExpandoObject _result;

    void Establish()
    {
        _child = new
        {
            identity = new
            {
                key = new { tenant = new { id = "tenant" }, itemId = "item" }
            }
        }.AsExpandoObject();
        _key = new { tenant = new { id = "tenant" }, itemId = "item" }.AsExpandoObject();
    }

    void Because() => _result = new[] { _child }.FindByKey("identity.key", _key);

    [Fact] void should_find_the_child_by_the_nested_identity_path() => _result.ShouldEqual(_child);
}
