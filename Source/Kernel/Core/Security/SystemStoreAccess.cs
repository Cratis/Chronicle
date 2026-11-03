// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Allocation-free checks that decide whether a routing value addresses the System event store and whether it
/// is acceptable as a component of a grain key.
/// </summary>
public static class SystemStoreAccess
{
    /// <summary>
    /// The message used when a caller is not allowed to access the System event store.
    /// </summary>
    public const string Refused = "Access to the System event store requires administrator privileges.";

    /// <summary>
    /// The message used when a routing value contains the grain key separator.
    /// </summary>
    public const string SeparatorRefused = "The value must not contain '#'.";

    /// <summary>
    /// Checks whether an event store name addresses the System event store or one of its physical databases.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <returns>True if it is the System event store; false otherwise.</returns>
    public static bool IsSystem(string? eventStore) =>
        string.Equals(eventStore, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase) ||
        eventStore?.StartsWith("System+", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Checks whether a routing value contains the grain key separator.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if it contains the separator; false otherwise.</returns>
    public static bool HasKeySeparator(string? value) => value?.Contains(KeyHelper.Separator) == true;

    /// <summary>
    /// Finds the first routing value that contains the grain key separator.
    /// </summary>
    /// <param name="first">The first value.</param>
    /// <returns>The zero based index of the first violating value, or -1 if there is none.</returns>
    public static int FirstSeparatorViolation(string? first) => HasKeySeparator(first) ? 0 : -1;

    /// <summary>
    /// Finds the first routing value that contains the grain key separator.
    /// </summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    /// <returns>The zero based index of the first violating value, or -1 if there is none.</returns>
    public static int FirstSeparatorViolation(string? first, string? second)
    {
        if (HasKeySeparator(first)) return 0;
        return HasKeySeparator(second) ? 1 : -1;
    }

    /// <summary>
    /// Finds the first routing value that contains the grain key separator.
    /// </summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    /// <param name="third">The third value.</param>
    /// <returns>The zero based index of the first violating value, or -1 if there is none.</returns>
    public static int FirstSeparatorViolation(string? first, string? second, string? third)
    {
        var index = FirstSeparatorViolation(first, second);
        if (index >= 0) return index;
        return HasKeySeparator(third) ? 2 : -1;
    }

    /// <summary>
    /// Finds the first routing value that contains the grain key separator.
    /// </summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    /// <param name="third">The third value.</param>
    /// <param name="fourth">The fourth value.</param>
    /// <returns>The zero based index of the first violating value, or -1 if there is none.</returns>
    public static int FirstSeparatorViolation(string? first, string? second, string? third, string? fourth)
    {
        var index = FirstSeparatorViolation(first, second, third);
        if (index >= 0) return index;
        return HasKeySeparator(fourth) ? 3 : -1;
    }

    /// <summary>
    /// Finds the first routing value that contains the grain key separator.
    /// </summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    /// <param name="third">The third value.</param>
    /// <param name="fourth">The fourth value.</param>
    /// <param name="fifth">The fifth value.</param>
    /// <returns>The zero based index of the first violating value, or -1 if there is none.</returns>
    public static int FirstSeparatorViolation(string? first, string? second, string? third, string? fourth, string? fifth)
    {
        var index = FirstSeparatorViolation(first, second, third, fourth);
        if (index >= 0) return index;
        return HasKeySeparator(fifth) ? 4 : -1;
    }

    /// <summary>
    /// Finds the first routing value that contains the grain key separator.
    /// </summary>
    /// <param name="first">The first value.</param>
    /// <param name="second">The second value.</param>
    /// <param name="third">The third value.</param>
    /// <param name="fourth">The fourth value.</param>
    /// <param name="fifth">The fifth value.</param>
    /// <param name="sixth">The sixth value.</param>
    /// <returns>The zero based index of the first violating value, or -1 if there is none.</returns>
    public static int FirstSeparatorViolation(string? first, string? second, string? third, string? fourth, string? fifth, string? sixth)
    {
        var index = FirstSeparatorViolation(first, second, third, fourth, fifth);
        if (index >= 0) return index;
        return HasKeySeparator(sixth) ? 5 : -1;
    }
}
