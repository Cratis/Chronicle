// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Aksio.Cratis.Metrics;

/// <summary>
/// Represents a scope for metrics.
/// </summary>
/// <typeparam name="T">Type the scope is for.</typeparam>
public class MeterScope<T> : IMeterScope<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeterScope{T}"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> the scope is for.</param>
    /// <param name="tags">Tags associated with the scope.</param>
    public MeterScope(IMeter<T> meter, IDictionary<string, object> tags)
    {
        Meter = meter.ActualMeter;
        Tags = tags;
    }

    /// <inheritdoc/>
    public Meter Meter { get; }

    /// <inheritdoc/>
    public IDictionary<string, object> Tags { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
