// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The kind of value a part of a composite key holds, which decides how a decoded part is represented.
/// </summary>
public enum CompositeKeyPartKind
{
    /// <summary>
    /// Text, including identifiers and dates, which are represented as strings.
    /// </summary>
    Text = 0,

    /// <summary>
    /// A whole number.
    /// </summary>
    WholeNumber = 1,

    /// <summary>
    /// A number with a fractional part.
    /// </summary>
    Number = 2,

    /// <summary>
    /// A true or false value.
    /// </summary>
    Boolean = 3
}
