// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Holds up to four ordered routing values without a params array or heap allocation.
/// </summary>
/// <param name="First">The first routing value.</param>
/// <param name="Second">The second routing value, if present.</param>
/// <param name="Third">The third routing value, if present.</param>
/// <param name="Fourth">The fourth routing value, if present.</param>
internal readonly record struct ClientSpanValues(string First, string? Second = null, string? Third = null, string? Fourth = null)
{
    /// <summary>
    /// Gets the exposed event source identifier, which is never supplied to a sampler.
    /// </summary>
    internal string? EventSourceId { get; init; }

    /// <summary>
    /// Creates inline sampling tags using the operation's precomputed keys.
    /// </summary>
    /// <param name="keys">The ordered convention keys for the operation.</param>
    /// <returns>The sampling tags.</returns>
    internal TagList SamplingTags(string[] keys)
    {
        TagList tags = default;
        tags.Add(keys[0], First);
        if (Second is { } second) tags.Add(keys[1], second);
        if (Third is { } third) tags.Add(keys[2], third);
        if (Fourth is { } fourth) tags.Add(keys[3], fourth);
        return tags;
    }

    /// <summary>
    /// Adds the operation's legacy attributes after the activity starts, as the generated spans did.
    /// </summary>
    /// <param name="activity">The started activity, if sampled.</param>
    /// <param name="keys">The ordered legacy keys for the operation.</param>
    internal void SetTags(Activity? activity, string[] keys)
    {
        if (activity is null) return;
        activity.SetTag(keys[0], First);
        if (Second is { } second) activity.SetTag(keys[1], second);
        if (Third is { } third) activity.SetTag(keys[2], third);
        if (Fourth is { } fourth) activity.SetTag(keys[3], fourth);
    }
}
