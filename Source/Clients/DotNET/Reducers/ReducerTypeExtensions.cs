// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Extension methods for working with reducer types.
/// </summary>
public static class ReducerTypeExtensions
{
    /// <summary>
    /// Get the reducer id for a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="ReducerId"/> for the type.</returns>
    public static ReducerId GetReducerId(this Type type)
    {
        var reducerAttribute = type.GetCustomAttribute<ReducerAttribute>();
        var id = reducerAttribute?.Id.Value ?? string.Empty;
        return id switch
        {
            "" => new ReducerId(type.FullName ?? $"{type.Namespace}.{type.Name}"),
            _ => new ReducerId(id)
        };
    }
}
