// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_a_null_parent : Specification
{
    ExpandoObject _root;
    ExpandoObject _parent;

    void Establish()
    {
        _root = new ExpandoObject();
        ((IDictionary<string, object?>)_root)["outer"] = null;
    }

    void Because() => _parent = _root.EnsurePath("outer.info.name", ArrayIndexers.NoIndexers);

    [Fact] void should_create_the_parent() => _parent.ShouldNotBeNull();
    [Fact] void should_store_the_recreated_outer_object() => ((IDictionary<string, object?>)_root)["outer"].ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_store_the_recreated_inner_object() => ReferenceEquals(((IDictionary<string, object?>)((IDictionary<string, object?>)_root)["outer"]!)["info"], _parent).ShouldBeTrue();
}
