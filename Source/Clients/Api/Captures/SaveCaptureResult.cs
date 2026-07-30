// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents the result of saving a capture.
/// </summary>
public class SaveCaptureResult
{
    /// <summary>
    /// Gets or sets the saved <see cref="Capture"/> - null when the save was rejected.
    /// </summary>
    public Capture? Capture { get; set; }

    /// <summary>
    /// Gets or sets the messages explaining why the save was rejected - empty when the capture was saved.
    /// </summary>
    public IEnumerable<CaptureValidationMessage> Messages { get; set; } = [];
}
