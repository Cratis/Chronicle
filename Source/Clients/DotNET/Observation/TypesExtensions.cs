// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Reflection;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Extension methods for working with observers and type discovery.
/// </summary>
public static class TypesExtensions
{
    /// <summary>
    /// Find all observers.
    /// </summary>
    /// <param name="types">Collection of types.</param>
    /// <returns>Collection of types that are observers.</returns>
    public static IEnumerable<Type> AllObservers(this IEnumerable<Type> types) => types.Where(_ => _.HasAttribute<ObserverAttribute>()).ToArray();
}
