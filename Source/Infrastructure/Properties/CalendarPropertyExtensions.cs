// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Extension properties for deriving calendar values from a <see cref="DateTimeOffset"/> — the well-known derived
/// accessors usable inside a projection composite-key, event-context or event-content property accessor
/// expression (for example <c language="csharp">c => c.Occurred.Week</c>).
/// </summary>
/// <remarks>
/// <para>
/// See <see cref="DerivedPropertyFunctions"/> for how these are recognized inside an accessor expression,
/// and for the pattern to follow when adding another derived accessor.
/// </para>
/// <para>
/// These are implemented as extension methods that read like properties in composite-key expressions.
/// To callers, they appear as property accessors on <see cref="DateTimeOffset"/> even though the type
/// itself doesn't carry them natively.
/// </para>
/// </remarks>
public static class CalendarPropertyExtensions
{
    /// <summary>
    /// Gets the ISO 8601 week-of-year number for a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="dateTimeOffset">The value to get the ISO week number for.</param>
    /// <returns>The ISO 8601 week number, from 1 through 53.</returns>
    /// <remarks>
    /// <para>
    /// This extension method exposes the ISO week number (1–53) as a computed property accessor usable in
    /// projection composite-key expressions (for example <c language="csharp">c => c.Occurred.Week</c>) without
    /// requiring a separate event property or translator reactor.
    /// </para>
    /// </remarks>
    public static int Week(this DateTimeOffset dateTimeOffset) => System.Globalization.ISOWeek.GetWeekOfYear(dateTimeOffset.DateTime);
}
