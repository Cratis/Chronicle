// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Records directly with a concrete value-type recorder and cached scope tags.
/// </summary>
/// <typeparam name="T">The measurement type.</typeparam>
/// <typeparam name="TRecorder">The concrete instrument recorder.</typeparam>
/// <param name="recorder">The precreated instrument's recorder.</param>
/// <param name="tags">The cached scope tags.</param>
/// <param name="firstKey">The preselected first additional tag key.</param>
/// <param name="secondKey">The preselected second additional tag key.</param>
internal sealed class SingleMetricInstrument<T, TRecorder>(TRecorder recorder, TagList tags, string firstKey, string secondKey)
    where T : struct
    where TRecorder : struct, IMetricRecorder<T>
{
    /// <summary>Records a measurement with the cached scope tags.</summary>
    /// <param name="value">The measurement.</param>
    internal void Record(T value) => recorder.Record(value, in tags);

    /// <summary>Records a measurement with an additional tag.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="tag">The additional tag value.</param>
    internal void Record(T value, object? tag)
    {
        var measurementTags = tags;
        measurementTags.Add(firstKey, tag);
        recorder.Record(value, in measurementTags);
    }

    /// <summary>Records a measurement with two additional tags.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="firstTag">The first additional tag value.</param>
    /// <param name="secondTag">The second additional tag value.</param>
    internal void Record(T value, object? firstTag, object? secondTag)
    {
        var measurementTags = tags;
        measurementTags.Add(firstKey, firstTag);
        measurementTags.Add(secondKey, secondTag);
        recorder.Record(value, in measurementTags);
    }
}
