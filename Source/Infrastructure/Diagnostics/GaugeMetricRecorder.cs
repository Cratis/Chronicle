// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>Records on a precreated gauge without a recording delegate.</summary>
/// <typeparam name="T">The measurement type.</typeparam>
/// <param name="gauge">The precreated gauge, or null for disabled metrics.</param>
internal readonly struct GaugeMetricRecorder<T>(Gauge<T>? gauge) : IMetricRecorder<T>
    where T : struct
{
#pragma warning disable RCS1242 // Forwarded to the BCL's in TagList overload without instance member access or a defensive copy.
    /// <inheritdoc/>
    public void Record(T value, in TagList tags) => gauge?.Record(value, in tags);
#pragma warning restore RCS1242
}
