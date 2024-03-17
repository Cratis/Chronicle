// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Types;

/// <summary>
/// Represents an implementation of <see cref="IInstancesOf{T}"/>.
/// </summary>
/// <typeparam name="T">Base type to discover for - must be an abstract class or an interface.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="InstancesOf{T}"/> class.
/// </remarks>
/// <param name="types"><see cref="ITypes"/> used for finding types.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> used for managing instances of the types when needed.</param>
public class InstancesOf<T>(ITypes types, IServiceProvider serviceProvider) : IInstancesOf<T>
    where T : class
{
    readonly IEnumerable<Type> _types = types.FindMultiple<T>();

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var type in _types) yield return (T)serviceProvider.GetService(type)!;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var type in _types) yield return serviceProvider.GetService(type);
    }
}
