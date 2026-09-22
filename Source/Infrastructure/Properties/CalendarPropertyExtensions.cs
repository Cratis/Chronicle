// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Extension properties for deriving calendar values from a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implemented using C# 12 extension types, these properties are directly accessible on
/// <see cref="DateTimeOffset"/> values in composite-key expressions without requiring
/// translator reactors or new event types.
/// </para>
/// </remarks>
public static class CalendarPropertyExtensions
{
    /// <summary>
    /// Extension type providing calendar accessors on <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="dateTimeOffset">The <see cref="DateTimeOffset"/> value to extend.</param>
#pragma warning disable CA1034
    extension(DateTimeOffset dateTimeOffset)
#pragma warning restore CA1034
    {
        /// <summary>
        /// Gets the ISO 8601 week-of-year number (1–53).
        /// </summary>
        public int Week => System.Globalization.ISOWeek.GetWeekOfYear(dateTimeOffset.DateTime);
    }
}
