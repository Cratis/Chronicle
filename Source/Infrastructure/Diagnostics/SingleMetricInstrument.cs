// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Records directly with a concrete value-type recorder and cached scope tags.
/// </summary>
/// <remarks>
/// The scope tags are shared with every other instrument of the scope rather than copied into each instrument, and are
/// never changed. Recording with additional tags appends them to <see cref="MetricScopeTags.ForAppending"/>, a local copy
/// that shares nothing with the scope, so concurrent and nested recordings cannot see each other's tags.
/// </remarks>
/// <typeparam name="T">The measurement type.</typeparam>
/// <typeparam name="TRecorder">The concrete instrument recorder.</typeparam>
/// <param name="recorder">The precreated instrument's recorder.</param>
/// <param name="tags">The cached scope tags, shared by the scope's instruments.</param>
/// <param name="firstKey">The preselected first additional tag key; empty when the naming mode has no such key.</param>
/// <param name="secondKey">The preselected second additional tag key; empty when the naming mode has no such key.</param>
internal sealed class SingleMetricInstrument<T, TRecorder>(TRecorder recorder, MetricScopeTags tags, string firstKey, string secondKey)
    where T : struct
    where TRecorder : struct, IMetricRecorder<T>
{
    /// <summary>Records a measurement with the cached scope tags.</summary>
    /// <param name="value">The measurement.</param>
    internal void Record(T value) => recorder.Record(value, in tags.Tags);

    /// <summary>Records a measurement with an additional tag.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="tag">The additional tag value.</param>
    internal void Record(T value, object? tag)
    {
        var measurementTags = tags.ForAppending();
        if (firstKey.Length > 0) measurementTags.Add(firstKey, tag);
        recorder.Record(value, in measurementTags);
    }

    /// <summary>Records a measurement with two additional tags.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="firstTag">The first additional tag value.</param>
    /// <param name="secondTag">The second additional tag value.</param>
    internal void Record(T value, object? firstTag, object? secondTag)
    {
        var measurementTags = tags.ForAppending();
        if (firstKey.Length > 0) measurementTags.Add(firstKey, firstTag);
        if (secondKey.Length > 0) measurementTags.Add(secondKey, secondTag);
        recorder.Record(value, in measurementTags);
    }
}
