// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Reflection;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.Observation;

/// <summary>
/// Extension methods for working with observers and type discovery.
/// </summary>
public static class TypesExtensions
{
    /// <summary>
    /// Find all observers.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> to extend.</param>
    /// <returns>Collection of types that are observers.</returns>
    public static IEnumerable<Type> AllObservers(this ITypes types) => types.All.Where(_ => _.HasAttribute<ObserverAttribute>()).ToArray();
}
