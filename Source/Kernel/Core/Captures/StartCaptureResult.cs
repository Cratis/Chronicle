// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the outcome of starting a capture.
/// </summary>
/// <param name="Messages">What validating the capture had to say.</param>
public record StartCaptureResult(IEnumerable<Contracts.Captures.CaptureValidationMessage> Messages);
