// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Publishes each measurement to two independently precreated instrument sets during a migration.
/// </summary>
/// <typeparam name="T">The measurement type.</typeparam>
/// <param name="legacy">The legacy instrument and cached tags.</param>
/// <param name="convention">The convention instrument and cached tags.</param>
internal sealed class DualMetricInstrument<T>(IMetricInstrument<T> legacy, IMetricInstrument<T> convention) : IMetricInstrument<T>
    where T : struct
{
    /// <inheritdoc/>
    public void Record(T value)
    {
        legacy.Record(value);
        convention.Record(value);
    }

    /// <inheritdoc/>
    public void Record(T value, object? tag)
    {
        legacy.Record(value, tag);
        convention.Record(value, tag);
    }

    /// <inheritdoc/>
    public void Record(T value, object? firstTag, object? secondTag)
    {
        legacy.Record(value, firstTag, secondTag);
        convention.Record(value, firstTag, secondTag);
    }
}
