// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Collections;

/// <summary>
/// Exception that gets thrown when an element is already added to a binary tree.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ItemAlreadyAddedToBinaryTree"/> class.
/// </remarks>
/// <param name="item">Element already added.</param>
public class ItemAlreadyAddedToBinaryTree(object item) : Exception($"Item '{item}' has already been added to the tree.");
