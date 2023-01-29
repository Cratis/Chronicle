// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Aksio.Cratis.Collections;

/// <summary>
/// Represents a binary tree.
/// </summary>
/// <typeparam name="TItem">Type of items in the binary tree.</typeparam>
public class BinaryTree<TItem> : ICollection<TItem>
{
    readonly IComparer<TItem> _comparer;

    /// <summary>
    /// Gets the root of the tree.
    /// </summary>
    public BinaryTreeNode<TItem>? Root { get; private set; }

    /// <inheritdoc/>
    public int Count { get; private set; }

    /// <inheritdoc/>
    public bool IsReadOnly => throw new NotImplementedException();

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryTree{TElement}"/> class.
    /// </summary>
    public BinaryTree() : this(Comparer<TItem>.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryTree{TElement}"/> class.
    /// </summary>
    /// <param name="comparer">The <see cref="IComparer{T}"/> to use.</param>
    public BinaryTree(IComparer<TItem> comparer)
    {
        _comparer = comparer;
    }

    /// <inheritdoc/>
    public void Add(TItem item)
    {
        if (Root is null)
        {
            Root = new BinaryTreeNode<TItem>(item, null, null);
            Count = 1;
            return;
        }

        AddInternal(
            Root,
            item,
            root => Root = root,
            left => Root.Left = left,
            right => Root.Right = right);
    }

    /// <inheritdoc/>
    public void Clear() => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool Contains(TItem item) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void CopyTo(TItem[] array, int arrayIndex) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEnumerator<TItem> GetEnumerator() => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool Remove(TItem item)
    {
        return true;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();


    void AddInternal(
        BinaryTreeNode<TItem> current,
        TItem item,
        Action<BinaryTreeNode<TItem>> setRoot,
        Action<BinaryTreeNode<TItem>> setLeft,
        Action<BinaryTreeNode<TItem>> setRight)
    {
        var comparison = _comparer.Compare(item, current.Value);
        if (comparison == 0)
        {
            throw new ItemAlreadyAddedToBinaryTree(item!);
        }

        if (comparison < 0)
        {
            setRoot(new BinaryTreeNode<TItem>(item, current, null));
        }
        else
        {
            if (current.Left is not null)
            {
                var leftComparison = _comparer.Compare(item, current.Left.Value);
                if (current.Right is not null)
                {
                    var rightComparison = _comparer.Compare(item, current.Right.Value);
                    if (leftComparison > 0 && rightComparison < 0)
                    {
                        AddInternal(
                            current.Right,
                            item,
                            root => setRight(root),
                            left => setRight(left),
                            right => setRight(right));

                        return;
                    }
                }

                AddInternal(
                    current.Left,
                    item,
                    root => setLeft(root),
                    left => setRight(left),
                    right => setRight(right));
            }
            else
            {
                setLeft(new BinaryTreeNode<TItem>(item, null, null));
            }
        }

        Count++;
    }
}
