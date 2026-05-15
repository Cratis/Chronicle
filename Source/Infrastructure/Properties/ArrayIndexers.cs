// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Represents an implementation of <see cref="ArrayIndexers"/>.
/// </summary>
public class ArrayIndexers
{
    /// <summary>
    /// Represents no indexers - used when you don't have any indexers.
    /// </summary>
    public static readonly ArrayIndexers NoIndexers = new([]);

    readonly List<ArrayIndexer> _arrayIndexers = [];

    int? _computedHashCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayIndexers"/> class.
    /// </summary>
    public ArrayIndexers()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayIndexers"/> class.
    /// </summary>
    /// <param name="arrayIndexers">A collection of <see cref="ArrayIndexer">array indexers</see>.</param>
    public ArrayIndexers(IEnumerable<ArrayIndexer> arrayIndexers)
    {
        All = arrayIndexers;
    }

    /// <summary>
    /// Gets the count of <see cref="ArrayIndexer"/>.
    /// </summary>
    public int Count => _arrayIndexers.Count;

    /// <summary>
    /// Gets a value indicating whether it is empty.
    /// </summary>
    public bool IsEmpty => _arrayIndexers.Count == 0;

    /// <summary>
    /// Gets all <see cref="ArrayIndexer"/>.
    /// </summary>
    /// <returns>Collection of <see cref="ArrayIndexer"/>.</returns>
    public IEnumerable<ArrayIndexer> All
    {
        get => _arrayIndexers;
        init => _arrayIndexers = value.ToList();
    }

    /// <summary>
    /// Get an <see cref="ArrayIndexer"/> for a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <returns>The <see cref="ArrayIndexer"/>.</returns>
    /// <exception cref="MissingArrayIndexerForPropertyPath">Thrown when no matching <see cref="ArrayIndexer"/> exists for <paramref name="propertyPath"/>.</exception>
    public ArrayIndexer GetFor(PropertyPath propertyPath)
        => FindFor(propertyPath) ?? throw new MissingArrayIndexerForPropertyPath(propertyPath);

    /// <summary>
    /// Check if there is an <see cref="ArrayIndexer"/> for a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to check for.</param>
    /// <returns>True if it has it, false if not.</returns>
    public bool HasFor(PropertyPath propertyPath) => FindFor(propertyPath) is not null;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is ArrayIndexers other)
        {
            return All.SequenceEqual(other.All);
        }

        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // From https://stackoverflow.com/a/8094931
        unchecked
        {
            _computedHashCode ??= All.Aggregate(19, (current, item) => (current * 31) + item.GetHashCode());
            return _computedHashCode.Value;
        }
    }

    ArrayIndexer? FindFor(PropertyPath propertyPath)
    {
        for (var index = _arrayIndexers.Count - 1; index >= 0; index--)
        {
            var candidate = _arrayIndexers[index];
            if (candidate.ArrayProperty == propertyPath)
            {
                return candidate;
            }
        }

        return null;
    }
}
