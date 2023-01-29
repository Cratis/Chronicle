// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Collections.for_BinaryTree;

public class when_adding_item_less_than_root : Specification
{
    BinaryTree<int> tree;

    void Establish()
    {
        tree = new BinaryTree<int>
        {
            42
        };
    }

    void Because() => tree.Add(41);

    [Fact] void should_have_two_elements() => tree.Count.ShouldEqual(2);
    [Fact] void should_have_root_with_value() => tree.Root!.Value.ShouldEqual(41);
    [Fact] void should_have_root_with_left_with_value() => tree.Root!.Left!.Value.ShouldEqual(42);
    [Fact] void should_have_root_with_no_right() => tree.Root!.Right.ShouldBeNull();
}
