// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts;

/// <summary>
/// Converts actual incident positions to signed 64-bit storage values.
/// </summary>
public static class AlertIncidentSequenceNumberConverters
{
    /// <summary>
    /// Validates and converts a position to SQL.
    /// </summary>
    /// <param name="value">The position.</param>
    /// <returns>The signed position.</returns>
    public static long ToSql(EventSequenceNumber value)
    {
        AlertIncidentStorageRules.Validate(value);

        return (long)value.Value;
    }

    /// <summary>
    /// Converts a stored position to the kernel value.
    /// </summary>
    /// <param name="value">The stored position.</param>
    /// <returns>The kernel position.</returns>
    public static EventSequenceNumber ToKernel(long value) => new((ulong)value);
}
