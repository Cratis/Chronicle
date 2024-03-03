// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Metrics;

/// <summary>
/// Defines a meter for a specific type.
/// </summary>
/// <typeparam name="T">Type the meter is for.</typeparam>
public interface IMeter<T>
{
    /// <summary>
    /// Gets the actual <see cref="Meter"/> instance.
    /// </summary>
    Meter ActualMeter { get; }
}
