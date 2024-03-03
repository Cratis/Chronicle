// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Metrics;

/// <summary>
/// Represents a scope for metrics.
/// </summary>
/// <typeparam name="T">Type the scope is for.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="MeterScope{T}"/> class.
/// </remarks>
/// <param name="meter">The <see cref="IMeter{T}"/> the scope is for.</param>
/// <param name="tags">Tags associated with the scope.</param>
public class MeterScope<T>(IMeter<T> meter, IDictionary<string, object> tags) : IMeterScope<T>
{
    /// <inheritdoc/>
    public Meter Meter { get; } = meter.ActualMeter;

    /// <inheritdoc/>
    public IDictionary<string, object> Tags { get; } = tags;

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
