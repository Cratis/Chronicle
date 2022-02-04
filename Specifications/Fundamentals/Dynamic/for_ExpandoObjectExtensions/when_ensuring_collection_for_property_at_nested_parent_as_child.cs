// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions
{
    public class when_ensuring_collection_for_property_at_nested_parent_as_child : Specification
    {
        const string innerCollectionProperty = "myCollection";
        const string property = $"firstCollection.{innerCollectionProperty}";
        const string parentKey = "d4e23066-1508-40eb-be58-a16abc6c572f";
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
            ((IDictionary<string, object>)parentAsChild)[parentIdentifierProperty] = parentKey;
            ((dynamic)root).firstCollection = new ExpandoObject[] {
                otherParentAsChild,
                parentAsChild
            };
        }

        void Because() => result = root.EnsureCollection<ExpandoObject>(property, parentIdentifierProperty, parentKey);

        [Fact] void should_add_collection_to_parent() => ((IDictionary<string, object>)parentAsChild)[innerCollectionProperty].ShouldEqual(result);
        [Fact] void should_create_a_collection() => result.ShouldNotBeNull();
    }
}
