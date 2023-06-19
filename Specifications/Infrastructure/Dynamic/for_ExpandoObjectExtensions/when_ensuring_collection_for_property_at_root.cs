// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_collection_for_property_at_root : Specification
{
    const string property = "myCollection";
    ExpandoObject root;
    ICollection<ExpandoObject> result;

    void Establish() => root = new();

    void Because() => result = root.EnsureCollection<ExpandoObject>(property, ArrayIndexers.NoIndexers);

    [Fact] void should_add_collection_to_root() => ((IDictionary<string, object>)root)[property].ShouldEqual(result);
    [Fact] void should_create_a_collection() => result.ShouldNotBeNull();
}
