// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Aksio.Cratis.Metrics;

/// <summary>
/// Represents a typed <see cref="Meter"/>.
/// </summary>
/// <typeparam name="T">Type the meter is for.</typeparam>
public class Meter<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Meter{T}"/> class.
    /// </summary>
    /// <param name="meter">The actual meter being used.</param>
    public Meter(Meter meter)
    {
        ActualMeter = meter;
    }

    /// <summary>
    /// Gets the actual <see cref="Meter"/> instance.
    /// </summary>
    public Meter ActualMeter { get; }
}
