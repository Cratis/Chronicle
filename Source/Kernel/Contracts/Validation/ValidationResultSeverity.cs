// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Validation;

/// <summary>
/// Wire-level representation of the severity of a validation result.
/// </summary>
public enum ValidationResultSeverity
{
    /// <summary>
    /// The severity is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The result is informational.
    /// </summary>
    Information = 1,

    /// <summary>
    /// The result is a warning.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// The result is an error.
    /// </summary>
    Error = 3
}
