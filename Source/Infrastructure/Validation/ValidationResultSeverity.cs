// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Validation;

/// <summary>
/// Holds the values for severity of a validation result.
/// </summary>
public enum ValidationResultSeverity
{
    /// <summary>
    /// The validation result is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The validation result is a warning.
    /// </summary>
    Information = 1,

    /// <summary>
    /// The validation result is a warning.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// The validation result is an error.
    /// </summary>
    Error = 3
}
