// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Records on instruments and tag keys selected when a producer is constructed.
/// </summary>
/// <typeparam name="T">The measurement type.</typeparam>
internal interface IMetricInstrument<T>
    where T : struct
{
    /// <summary>Records a measurement with the cached scope tags.</summary>
    /// <param name="value">The measurement.</param>
    void Record(T value);

    /// <summary>Records a measurement with one additional value under a cached tag key.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="tag">The tag value.</param>
    void Record(T value, object? tag);

    /// <summary>Records a measurement with two additional values under cached tag keys.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="firstTag">The first tag value.</param>
    /// <param name="secondTag">The second tag value.</param>
    void Record(T value, object? firstTag, object? secondTag);
}
