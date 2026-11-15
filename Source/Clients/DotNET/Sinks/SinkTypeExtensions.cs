// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Extension methods for finding the named sink a type stores to.
/// </summary>
public static class SinkTypeExtensions
{
    /// <summary>
    /// Get the name of the sink a type declares with <see cref="SinkAttribute"/>.
    /// </summary>
    /// <param name="type">The type to get for.</param>
    /// <returns>The name, or null when the type declares none.</returns>
    public static string? GetSinkName(this Type type) =>
        type.GetCustomAttribute<SinkAttribute>() is { Name: { } name } && !string.IsNullOrWhiteSpace(name) ? name.Trim() : null;
}
