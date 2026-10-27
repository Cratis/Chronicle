// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal record a_sample_message
{
    /// <summary>
    /// Gets the nullable names, including nullable elements.
    /// </summary>
    public List<string?>? Names { get; init; }

    /// <summary>
    /// Gets the value with a nonpublic setter.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value by index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The index value.</returns>
    public int this[int index] => index;

    /// <summary>
    /// Preserves nullable optional parameters and defaults without executing a handler.
    /// </summary>
    /// <param name="filter">The optional filter.</param>
    /// <param name="limit">The optional limit.</param>
    /// <returns>The filter.</returns>
    public static string? Filter(string? filter = null, int limit = 7) => filter;

    /// <summary>
    /// Exercises generic constraints and parameter modifiers without executing a handler.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="count">The count.</param>
    /// <param name="value">The formatted count.</param>
    /// <param name="items">The variadic items.</param>
    /// <returns>A sample message.</returns>
    public static a_sample_message Create<T>(in int count, out string value, params T[] items)
        where T : class, new()
    {
        value = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new a_sample_message { Names = items.Select(item => item.ToString()).ToList() };
    }
}
