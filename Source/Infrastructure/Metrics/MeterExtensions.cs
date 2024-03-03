// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Metrics;

/// <summary>
/// Extension methods for working with meters.
/// </summary>
public static class MeterExtensions
{
    /// <summary>
    /// Begin a scope for a meter.
    /// </summary>
    /// <param name="meter">The meter to begin scope for.</param>
    /// <param name="tags">The tags associated with the scope.</param>
    /// <typeparam name="T">Type the scope is for.</typeparam>
    /// <returns>A new <see cref="IMeterScope{T}"/>.</returns>
    public static IMeterScope<T> BeginScope<T>(this IMeter<T> meter, IDictionary<string, object> tags)
    {
        return new MeterScope<T>(meter, tags);
    }
}
