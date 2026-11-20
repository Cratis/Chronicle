// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>A load average reading.</summary>
/// <param name="OneMinute">The one minute load average.</param>
/// <param name="FiveMinute">The five minute load average.</param>
/// <param name="FifteenMinute">The fifteen minute load average.</param>
public sealed record LoadReading(double OneMinute, double FiveMinute, double FifteenMinute)
{
    /// <summary>Gets a reading that carries no information, used where the platform cannot report load.</summary>
    public static LoadReading Unknown { get; } = new(double.NaN, double.NaN, double.NaN);
}

