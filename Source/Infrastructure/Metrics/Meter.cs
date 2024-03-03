// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Metrics;

/// <summary>
/// Represents a typed <see cref="Meter"/>.
/// </summary>
/// <typeparam name="T">Type the meter is for.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="Meter{T}"/> class.
/// </remarks>
/// <param name="meter">The actual meter being used.</param>
public class Meter<T>(Meter meter) : IMeter<T>
{
    /// <inheritdoc/>
    public Meter ActualMeter { get; } = meter;
}
