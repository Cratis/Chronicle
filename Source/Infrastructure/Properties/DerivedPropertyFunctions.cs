// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Properties;

/// <summary>
/// Holds the well-known <see cref="DerivedPropertyFunction"/> instances Chronicle recognizes when parsing and
/// evaluating a projection composite-key, event-context or event-content property accessor expression whose
/// terminal segment is a computed property rather than a plain member access — for example
/// <c language="csharp">c => c.Occurred.Week</c>.
/// </summary>
/// <remarks>
/// <para>
/// The client's <c language="csharp">ExpressionExtensions.TryGetPropertyPath</c> rejects an arbitrary method call rooted at the
/// lambda parameter — it would have to execute the expression to know the value, and a property path is
/// extracted at definition time, not executed. A call recognized by this registry is the deliberate, narrow
/// exception: it is folded into the <see cref="PropertyPath"/> as a <see cref="DerivedPropertyFunctionSegment"/>
/// and <see cref="PropertyPath.GetValue"/> evaluates it against the value the rest of the path resolved to,
/// rather than reflecting for a property that does not exist.
/// </para>
/// <para>This is the extension point for adding a new derived accessor beyond the well-known set:</para>
/// <list type="number">
/// <item><description>Add a public, zero-argument extension method for it — see
/// <see cref="CalendarPropertyExtensions.Week(DateTimeOffset)"/> for the pattern to follow. It should read
/// like a property access inside accessor expressions (e.g., `.Week` not `.GetWeek()`).</description></item>
/// <item><description>Register a <see cref="DerivedPropertyFunction"/> under that same name in <see cref="All"/>,
/// with <see cref="DerivedPropertyFunction.Evaluate"/> delegating straight to the extension method so the two can
/// never drift apart.</description></item>
/// </list>
/// <para>
/// The extension method is parsed as part of the property path but never executed when it appears inside an
/// accessor expression passed to <c language="csharp">TryGetPropertyPath</c> — only the expression tree is
/// inspected. It runs through <see cref="DerivedPropertyFunction.Evaluate"/>, once per evaluation, against the
/// value the rest of the path resolved to.
/// </para>
/// </remarks>
public static class DerivedPropertyFunctions
{
    /// <summary>
    /// All well-known derived functions, keyed by <see cref="DerivedPropertyFunction.Name"/>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, DerivedPropertyFunction> All = new Dictionary<string, DerivedPropertyFunction>
    {
        [nameof(CalendarPropertyExtensions.Week)] = new(
            nameof(CalendarPropertyExtensions.Week),
            value => ToDateTimeOffset(value).Week())
    };

    /// <summary>
    /// Try to get the <see cref="DerivedPropertyFunction"/> registered under a given name.
    /// </summary>
    /// <param name="name">Name to look for.</param>
    /// <param name="function">The <see cref="DerivedPropertyFunction"/>, if found.</param>
    /// <returns>True if found, false if not.</returns>
    public static bool TryGet(string name, out DerivedPropertyFunction function) => All.TryGetValue(name, out function!);

    static DateTimeOffset ToDateTimeOffset(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
        DateOnly dateOnly => new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
        string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture),
        _ => throw new UnableToDeriveCalendarValue(value.GetType())
    };
}
