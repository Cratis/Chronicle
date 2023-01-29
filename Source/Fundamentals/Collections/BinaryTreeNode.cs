// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Collections;

/// <summary>
/// Represents a node in a binary tree.
/// </summary>
/// <typeparam name="TItem">Type of element.</typeparam>
public class BinaryTreeNode<TItem>
{
    /// <summary>
    /// Gets the value of the node.
    /// </summary>
    public TItem Value { get; init; }

    /// <summary>
    /// Gets the left branch.
    /// </summary>
    public BinaryTreeNode<TItem>? Left { get; set; }

    /// <summary>
    /// Gets the right branch.
    /// </summary>
    public BinaryTreeNode<TItem>? Right { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryTreeNode{TElement}"/> class.
    /// </summary>
    /// <param name="value">The value of the node.</param>
    /// <param name="left">The left branch.</param>
    /// <param name="right">The right branch.</param>
    public BinaryTreeNode(TItem value, BinaryTreeNode<TItem>? left, BinaryTreeNode<TItem>? right)
    {
        Value = value;
        Left = left;
        Right = right;
    }
}
