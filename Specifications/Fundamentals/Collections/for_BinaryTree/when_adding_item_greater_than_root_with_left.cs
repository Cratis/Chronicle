// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Collections.for_BinaryTree;

public class when_adding_item_greater_than_root_with_left : Specification
{
    BinaryTree<int> tree;

    void Establish()
    {
        tree = new BinaryTree<int>
        {
            42,
            41
        };
    }

    void Because() => tree.Add(43);

    [Fact] void should_have_three_elements() => tree.Count.ShouldEqual(3);
    [Fact] void should_have_root_with_value() => tree.Root!.Value.ShouldEqual(42);
    [Fact] void should_have_root_with_left_with_value() => tree.Root!.Left!.Value.ShouldEqual(41);
    [Fact] void should_have_root_with_right_with_value() => tree.Root!.Right!.Value.ShouldEqual(43);
}
