// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Collections.for_BinaryTree;

public class when_adding_first_item : Specification
{
    BinaryTree<int> tree;

    void Establish() => tree = new BinaryTree<int>();

    void Because() => tree.Add(42);

    [Fact] void should_have_one_element() => tree.Count.ShouldEqual(1);
    [Fact] void should_have_root_with_value() => tree.Root!.Value.ShouldEqual(42);
    [Fact] void should_have_root_with_no_left() => tree.Root!.Left.ShouldBeNull();
    [Fact] void should_have_root_with_no_right() => tree.Root!.Right.ShouldBeNull();
}
