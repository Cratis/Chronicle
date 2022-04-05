// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Common;

/// <summary>
/// Represents an implementation of <see cref="IInstancesOf{T}"/> for providing exact instances.
/// </summary>
/// <typeparam name="T">Type of instances.</typeparam>
/// <remarks>
/// Useful for specs - as they need a predictable setup and outcome.
/// </remarks>
public class KnownInstancesOf<T> : IInstancesOf<T>
    where T : class
{
    readonly IEnumerable<T> _instances;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnownInstancesOf{T}"/> class.
    /// </summary>
    public KnownInstancesOf() : this(Array.Empty<T>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KnownInstancesOf{T}"/> class.
    /// </summary>
    /// <param name="instances">The known instances.</param>
    public KnownInstancesOf(params T[] instances)
    {
        _instances = instances;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KnownInstancesOf{T}"/> class.
    /// </summary>
    /// <param name="instances">The known instances.</param>
    public KnownInstancesOf(IEnumerable<T> instances)
    {
        _instances = instances;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => _instances.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _instances.GetEnumerator();
}
