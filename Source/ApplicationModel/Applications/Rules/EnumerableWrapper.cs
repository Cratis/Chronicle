// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Aksio.Cratis.Applications.Rules;

/// <summary>
/// Represents a wrapper for non generic enumerables, e.g. arrays.
/// </summary>
/// <typeparam name="T">Type expected to be returned.</typeparam>
public class EnumerableWrapper<T> : IEnumerable<T>
{
    class EnumeratorWrapper<TElement> : IEnumerator<TElement>
    {
        readonly IEnumerator _enumerator;
        object IEnumerator.Current => Current!;
        public TElement Current => (TElement)_enumerator.Current;

        public EnumeratorWrapper(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public void Dispose()
        {
        }

        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => _enumerator.Reset();
    }

    readonly IEnumerable _enumerable;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableWrapper{T}"/> class.
    /// </summary>
    /// <param name="enumerable">The <see cref="IEnumerable"/> to wrap.</param>
    public EnumerableWrapper(IEnumerable enumerable)
    {
        _enumerable = enumerable;
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => new EnumeratorWrapper<T>(_enumerable.GetEnumerator());

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _enumerable.GetEnumerator();
}
