// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Extension methods for deriving calendar values from a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// <para>
/// These are accessible on <see cref="DateTimeOffset"/> values in composite-key and event-context
/// expressions without requiring translator reactors or new event types.
/// </para>
/// <para>
/// They are deliberately extension <em>methods</em> rather than extension properties. The accessors
/// exist to be written inside an expression tree, and the language does not permit an extension
/// property there (compiler error CS9296, "An expression tree may not contain an extension property or
/// indexer access"), so a property form cannot be used for the one thing these exist for. An extension
/// method is legal in an expression tree and surfaces as the <see cref="System.Linq.Expressions.MethodCallExpression"/>
/// that the client's <c language="csharp">ExpressionExtensions.TryGetPropertyPath</c> matches against
/// <see cref="DerivedPropertyFunctions"/>.
/// </para>
/// </remarks>
public static class CalendarPropertyExtensions
{
    /// <summary>
    /// Gets the ISO 8601 week-of-year number (1-53).
    /// </summary>
    /// <param name="dateTimeOffset">The <see cref="DateTimeOffset"/> value to derive from.</param>
    /// <returns>The ISO 8601 week-of-year number.</returns>
    public static int Week(this DateTimeOffset dateTimeOffset) => System.Globalization.ISOWeek.GetWeekOfYear(dateTimeOffset.DateTime);
}
