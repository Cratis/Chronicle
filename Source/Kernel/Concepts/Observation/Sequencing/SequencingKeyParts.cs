// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Formats typed key parts independently of the current culture.
/// </summary>
public static class SequencingKeyParts
{
    /// <summary>
    /// Formats a Guid as lowercase dashed text.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The canonical text.</returns>
    public static string Canonical(Guid value) => value.ToString("D", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a signed 64-bit integer as invariant decimal text.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The canonical text.</returns>
    public static string Canonical(long value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats an unsigned 64-bit integer as invariant decimal text.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The canonical text.</returns>
    public static string Canonical(ulong value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a signed 32-bit integer as invariant decimal text.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The canonical text.</returns>
    public static string Canonical(int value) => value.ToString(CultureInfo.InvariantCulture);
}
