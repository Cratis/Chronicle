// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Types;

/// <summary>
/// Represents an implementation of <see cref="IImplementationsOf{T}"/>.
/// </summary>
/// <typeparam name="T">Base type to discover for - must be an abstract class or an interface.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ImplementationsOf{T}"/> class.
/// </remarks>
/// <param name="types"><see cref="ITypes"/> to use for finding types.</param>
public class ImplementationsOf<T>(ITypes types) : IImplementationsOf<T>
    where T : class
{
    readonly IEnumerable<Type> _types = types.FindMultiple<T>();

    /// <inheritdoc/>
    public IEnumerator<Type> GetEnumerator()
    {
        return _types.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _types.GetEnumerator();
    }
}
