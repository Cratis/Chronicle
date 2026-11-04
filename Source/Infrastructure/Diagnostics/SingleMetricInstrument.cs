// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Records directly without consulting configuration or translating tag keys on the measurement path.
/// </summary>
/// <typeparam name="T">The measurement type.</typeparam>
/// <param name="record">The precreated instrument's recording operation.</param>
/// <param name="tags">The cached scope tags.</param>
/// <param name="firstKey">The preselected first additional tag key.</param>
/// <param name="secondKey">The preselected second additional tag key.</param>
internal sealed class SingleMetricInstrument<T>(Action<T, TagList> record, TagList tags, string firstKey, string secondKey) : IMetricInstrument<T>
    where T : struct
{
    /// <inheritdoc/>
    public void Record(T value) => record(value, tags);

    /// <inheritdoc/>
    public void Record(T value, object? tag)
    {
        var measurementTags = tags;
        measurementTags.Add(firstKey, tag);
        record(value, measurementTags);
    }

    /// <inheritdoc/>
    public void Record(T value, object? firstTag, object? secondTag)
    {
        var measurementTags = tags;
        measurementTags.Add(firstKey, firstTag);
        measurementTags.Add(secondKey, secondTag);
        record(value, measurementTags);
    }
}
