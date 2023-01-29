// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Collections;

/// <summary>
/// Exception that gets thrown when an element is already added to a binary tree.
/// </summary>
public class ItemAlreadyAddedToBinaryTree : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemAlreadyAddedToBinaryTree"/> class.
    /// </summary>
    /// <param name="item">Element already added.</param>
    public ItemAlreadyAddedToBinaryTree(object item) : base($"Item '{item}' has already been added to the tree.")
    {
    }
}
