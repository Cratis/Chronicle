// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties;

/// <summary>
/// Represents an implementation of <see cref="ArrayIndexers"/>.
/// </summary>
public class ArrayIndexers
{
    /// <summary>
    /// Represents no indexers - used when you don't have any indexers.
    /// </summary>
    public static readonly ArrayIndexers NoIndexers = new(Enumerable.Empty<ArrayIndexer>());

    readonly IDictionary<PropertyPath, ArrayIndexer> _arrayIndexers = new Dictionary<PropertyPath, ArrayIndexer>();

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
    /// Gets all <see cref="ArrayIndexer"/>.
    /// </summary>
    /// <returns>Collection of <see cref="ArrayIndexer"/>.</returns>
    public IEnumerable<ArrayIndexer> All
    {
        get => _arrayIndexers.Values;
        init => _arrayIndexers = value.ToDictionary(_ => _.ArrayProperty, _ => _);
    }

    /// <summary>
    /// Get an <see cref="ArrayIndexer"/> for a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to get for.</param>
    /// <returns>The <see cref="ArrayIndexer"/>.</returns>
    public ArrayIndexer GetFor(PropertyPath propertyPath)
    {
        ThrowIfMissingArrayIndexerForPropertyPath(propertyPath);
        return _arrayIndexers[propertyPath];
    }

    /// <summary>
    /// Check if there is an <see cref="ArrayIndexer"/> for a <see cref="PropertyPath"/>.
    /// </summary>
    /// <param name="propertyPath"><see cref="PropertyPath"/> to check for.</param>
    /// <returns>True if it has it, false if not.</returns>
    public bool HasFor(PropertyPath propertyPath) => _arrayIndexers.ContainsKey(propertyPath);

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
    public override int GetHashCode() => base.GetHashCode();

    void ThrowIfMissingArrayIndexerForPropertyPath(PropertyPath propertyPath)
    {
        if (!_arrayIndexers.ContainsKey(propertyPath))
        {
            throw new MissingArrayIndexerForPropertyPath(propertyPath);
        }
    }
}
