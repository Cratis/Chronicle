// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents a command to validate a capture declaration.
/// </summary>
public class ValidateCaptureDeclaration
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [FromRoute]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capture declaration language source text to validate.
    /// </summary>
    public string Declaration { get; set; } = string.Empty;
}
