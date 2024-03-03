// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Metrics;

/// <summary>
/// Defines a scope for metrics.
/// </summary>
/// <typeparam name="T">Type the scope is for.</typeparam>
public interface IMeterScope<T> : IDisposable
{
    /// <summary>
    /// Gets the <see cref="Meter"/> associated with the scope.
    /// </summary>
    Meter Meter { get; }

    /// <summary>
    /// Gets the tags associated with the scope.
    /// </summary>
    IDictionary<string, object> Tags { get; }
}
