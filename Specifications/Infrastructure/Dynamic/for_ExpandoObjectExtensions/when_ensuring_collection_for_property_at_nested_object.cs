// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_collection_for_property_at_nested_object : Specification
{
    const string property = "deeply.nested.myCollection";
    ExpandoObject root;
    ICollection<ExpandoObject> result;

    void Establish() => root = new();

    void Because() => result = root.EnsureCollection<ExpandoObject>(property, ArrayIndexers.NoIndexers);

    [Fact] void should_add_collection_to_location() => ((object)((dynamic)root).deeply.nested.myCollection).ShouldEqual(result);
    [Fact] void should_create_a_collection() => result.ShouldNotBeNull();
}
