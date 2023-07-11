// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_collection_for_property_at_nested_parent_as_child : Specification
{
    const string first_collection_property = "firstCollection";
    const string inner_collection_property = "myCollection";

    const string property = $"[{first_collection_property}].[{inner_collection_property}]";
    const string parent_key = "d4e23066-1508-40eb-be58-a16abc6c572f";
    const string parentIdentifierProperty = "parentIdentifier";
    ExpandoObject root;
    ExpandoObject otherParentAsChild;
    ExpandoObject parentAsChild;
    ICollection<ExpandoObject> result;

    void Establish()
    {
        root = new();
        otherParentAsChild = new();
        ((IDictionary<string, object>)otherParentAsChild)[parentIdentifierProperty] = "2d025d4a-e579-40d4-889a-fc4fb7cd6016";
        parentAsChild = new();
        ((IDictionary<string, object>)parentAsChild)[parentIdentifierProperty] = parent_key;
        ((dynamic)root).firstCollection = new ExpandoObject[]
        {
            otherParentAsChild,
            parentAsChild
        };
    }

    void Because() => result = root.EnsureCollection<ExpandoObject>(property, new ArrayIndexers(new[] { new ArrayIndexer($"[{first_collection_property}]", parentIdentifierProperty, parent_key) }));

    [Fact] void should_add_collection_to_parent() => ((IDictionary<string, object>)parentAsChild)[inner_collection_property].ShouldEqual(result);
    [Fact] void should_create_a_collection() => result.ShouldNotBeNull();
}
