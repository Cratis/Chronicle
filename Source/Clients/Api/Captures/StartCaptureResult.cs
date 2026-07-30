// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents the result of starting a capture.
/// </summary>
public class StartCaptureResult
{
    /// <summary>
    /// Gets or sets the messages preventing the start - empty when the capture was started.
    /// </summary>
    public IEnumerable<CaptureValidationMessage> Messages { get; set; } = [];
}
