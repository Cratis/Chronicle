// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Records to the concrete instrument set selected at construction, with an optional migration copy.
/// </summary>
/// <typeparam name="T">The measurement type.</typeparam>
/// <typeparam name="TRecorder">The concrete instrument recorder.</typeparam>
/// <param name="primary">The selected instrument and cached tags.</param>
/// <param name="secondary">The optional convention instrument and cached tags in Both mode.</param>
internal sealed class MetricInstrument<T, TRecorder>(SingleMetricInstrument<T, TRecorder> primary, SingleMetricInstrument<T, TRecorder>? secondary = null)
    where T : struct
    where TRecorder : struct, IMetricRecorder<T>
{
    /// <summary>Records a measurement with the cached scope tags.</summary>
    /// <param name="value">The measurement.</param>
    internal void Record(T value)
    {
        primary.Record(value);
        secondary?.Record(value);
    }

    /// <summary>Records a measurement with an additional tag.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="tag">The additional tag value.</param>
    internal void Record(T value, object? tag)
    {
        primary.Record(value, tag);
        secondary?.Record(value, tag);
    }

    /// <summary>Records a measurement with two additional tags.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="firstTag">The first additional tag value.</param>
    /// <param name="secondTag">The second additional tag value.</param>
    internal void Record(T value, object? firstTag, object? secondTag)
    {
        primary.Record(value, firstTag, secondTag);
        secondary?.Record(value, firstTag, secondTag);
    }
}
