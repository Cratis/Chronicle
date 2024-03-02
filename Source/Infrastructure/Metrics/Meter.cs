// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Aksio.Cratis.Metrics;

/// <summary>
/// Represents a typed <see cref="Meter"/>.
/// </summary>
/// <typeparam name="T">Type the meter is for.</typeparam>
public class Meter<T> : IMeter<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Meter{T}"/> class.
    /// </summary>
    /// <param name="meter">The actual meter being used.</param>
    public Meter(Meter meter)
    {
        ActualMeter = meter;
    }

    /// <inheritdoc/>
    public Meter ActualMeter { get; }
}
