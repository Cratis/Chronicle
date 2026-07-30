// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents a message from compiling or validating a capture declaration.
/// </summary>
public class CaptureValidationMessage
{
    /// <summary>
    /// Gets or sets the message describing the problem.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the one-based line the problem relates to.
    /// </summary>
    public int Line { get; set; } = 1;

    /// <summary>
    /// Gets or sets the one-based column the problem relates to.
    /// </summary>
    public int Column { get; set; } = 1;
}
