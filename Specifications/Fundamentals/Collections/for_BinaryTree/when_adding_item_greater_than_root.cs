// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Collections.for_BinaryTree;

public class when_adding_item_greater_than_root : Specification
{
    BinaryTree<int> tree;

    void Establish()
    {
        tree = new BinaryTree<int>
        {
            42
        };
    }

    void Because() => tree.Add(43);

    [Fact] void should_have_two_elements() => tree.Count.ShouldEqual(2);
    [Fact] void should_have_root_with_value() => tree.Root!.Value.ShouldEqual(42);
    [Fact] void should_have_root_with_left_with_added_value() => tree.Root!.Left!.Value.ShouldEqual(43);
    [Fact] void should_have_root_with_no_right() => tree.Root!.Right.ShouldBeNull();
}


public class when_adding_a_random_collection_of_numbers : Specification
{
    BinaryTree<int> tree;

    void Establish() => tree = new BinaryTree<int>();

    void Because() => Enumerable.Range(0, 100).Select(_ => Random.Shared.Next(0, 1000)).Distinct().ForEach(tree.Add);

    [Fact] void should_have_the_same_number_of_elements_as_added() => tree.Count.ShouldEqual(100);
}
