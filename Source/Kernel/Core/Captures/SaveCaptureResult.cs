// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the outcome of saving a capture.
/// </summary>
/// <param name="Capture">The capture that was saved, absent when it was not.</param>
/// <param name="Messages">What compiling and validating the declaration had to say.</param>
public record SaveCaptureResult(
    CaptureDetails? Capture,
    IEnumerable<Contracts.Captures.CaptureValidationMessage> Messages);
